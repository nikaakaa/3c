using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using ThirdPersonPerformance.Instrumentation;

namespace ThirdPersonPerformance.Instrumentation.Editor
{
    internal static class PerformanceInstrumentationIlRewriter
    {
        public static PerformanceInstrumentationPointDescriptor Rewrite(
            ModuleDefinition module,
            MethodDefinition method,
            string metricId,
            string profilerName,
            PerformanceInstrumentationMode mode)
        {
            ulong metricHash = PerformanceInstrumentationWeaverConstants.Hash64(metricId);
            ulong pointId = PerformanceInstrumentationWeaverConstants.Hash64(
                module.Assembly.Name.Name + "|" + method.DeclaringType.FullName + "|" +
                method.FullName + "|" + metricId);
            FieldDefinition marker = AddMarkerField(
                module,
                method.DeclaringType,
                pointId,
                profilerName);
            string sourceFile = string.Empty;
            int sourceLine = 0;
            if (method.DebugInformation.HasSequencePoints && method.DebugInformation.SequencePoints.Count > 0)
            {
                SequencePoint sequencePoint = method.DebugInformation.SequencePoints[0];
                sourceFile = sequencePoint.Document?.Url ?? string.Empty;
                sourceLine = sequencePoint.StartLine;
            }
            WrapMethod(module, method, marker, pointId, metricHash, mode);
            return new PerformanceInstrumentationPointDescriptor(
                pointId,
                metricHash,
                metricId,
                method.DeclaringType.FullName,
                method.FullName,
                sourceFile,
                sourceLine);
        }

        static FieldDefinition AddMarkerField(
            ModuleDefinition module,
            TypeDefinition type,
            ulong pointId,
            string profilerName)
        {
            string fieldName = PerformanceInstrumentationWeaverConstants.GeneratedFieldPrefix +
                pointId.ToString("x16");
            FieldDefinition existing = type.Fields.FirstOrDefault(field => field.Name == fieldName);
            if (existing != null)
                return existing;

            TypeReference markerType = CreateMarkerType(module);
            var field = new FieldDefinition(
                fieldName,
                FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly,
                markerType);
            type.Fields.Add(field);
            MethodDefinition cctor = type.Methods.FirstOrDefault(method => method.IsConstructor && method.IsStatic);
            if (cctor == null)
            {
                cctor = new MethodDefinition(
                    ".cctor",
                    MethodAttributes.Private | MethodAttributes.Static |
                    MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    module.TypeSystem.Void);
                type.Methods.Add(cctor);
                cctor.Body = new MethodBody(cctor);
                cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }

            ILProcessor il = cctor.Body.GetILProcessor();
            Instruction first = cctor.Body.Instructions[0];
            var constructor = new MethodReference(".ctor", module.TypeSystem.Void, markerType)
            {
                HasThis = true
            };
            constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
            il.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, profilerName));
            il.InsertBefore(first, Instruction.Create(OpCodes.Newobj, constructor));
            il.InsertBefore(first, Instruction.Create(OpCodes.Stsfld, BindField(field)));
            return field;
        }

        static void WrapMethod(
            ModuleDefinition module,
            MethodDefinition method,
            FieldDefinition marker,
            ulong pointId,
            ulong metricId,
            PerformanceInstrumentationMode mode)
        {
            string originalName = PerformanceInstrumentationWeaverConstants.GeneratedMethodPrefix +
                pointId.ToString("x16");
            if (method.DeclaringType.Methods.Any(candidate => candidate.Name == originalName))
                return;

            MethodDefinition original = new MethodDefinition(
                originalName,
                MethodAttributes.Private | MethodAttributes.HideBySig |
                (method.IsStatic ? MethodAttributes.Static : 0),
                method.ReturnType);
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                ParameterDefinition parameter = method.Parameters[i];
                original.Parameters.Add(new ParameterDefinition(
                    parameter.Name,
                    parameter.Attributes,
                    parameter.ParameterType));
            }
            SequencePoint[] sequencePoints = method.DebugInformation.HasSequencePoints
                ? method.DebugInformation.SequencePoints.ToArray()
                : Array.Empty<SequencePoint>();
            original.Body = CloneBody(method, original);
            CopySequencePoints(sequencePoints, original);
            method.DeclaringType.Methods.Add(original);

            TypeReference runtimeType = new TypeReference(
                PerformanceInstrumentationWeaverConstants.RuntimeNamespace,
                PerformanceInstrumentationWeaverConstants.RuntimeTypeName,
                module,
                GetAssemblyReference(module, PerformanceInstrumentationWeaverConstants.RuntimeAssemblyName),
                false);
            TypeReference scopeType = new TypeReference(
                PerformanceInstrumentationWeaverConstants.RuntimeNamespace,
                PerformanceInstrumentationWeaverConstants.ScopeTypeName,
                module,
                GetAssemblyReference(module, PerformanceInstrumentationWeaverConstants.RuntimeAssemblyName),
                true);
            TypeReference endStateType = new TypeReference(
                PerformanceInstrumentationWeaverConstants.RuntimeNamespace,
                PerformanceInstrumentationWeaverConstants.EndStateTypeName,
                module,
                GetAssemblyReference(module, PerformanceInstrumentationIdentity.ContractsAssembly),
                true);
            TypeReference markerType = CreateMarkerType(module);
            string enterName = mode == PerformanceInstrumentationMode.Span
                ? "EnterSpan"
                : "EnterMarkerOnly";
            var enter = new MethodReference(enterName, scopeType, runtimeType)
            {
                HasThis = false
            };
            enter.Parameters.Add(new ParameterDefinition(module.TypeSystem.UInt64));
            if (mode == PerformanceInstrumentationMode.Span)
                enter.Parameters.Add(new ParameterDefinition(module.TypeSystem.UInt64));
            enter.Parameters.Add(new ParameterDefinition(markerType));
            var exit = new MethodReference("Exit", module.TypeSystem.Void, runtimeType)
            {
                HasThis = false
            };
            exit.Parameters.Add(new ParameterDefinition(new ByReferenceType(scopeType)));
            exit.Parameters.Add(new ParameterDefinition(endStateType));

            var wrapperBody = new MethodBody(method)
            {
                InitLocals = true,
                MaxStackSize = 8
            };
            var scope = new VariableDefinition(scopeType);
            var endState = new VariableDefinition(endStateType);
            wrapperBody.Variables.Add(scope);
            wrapperBody.Variables.Add(endState);
            VariableDefinition result = null;
            if (!method.ReturnType.FullName.Equals("System.Void", StringComparison.Ordinal))
            {
                result = new VariableDefinition(method.ReturnType);
                wrapperBody.Variables.Add(result);
            }

            ILProcessor il = wrapperBody.GetILProcessor();
            il.Append(Instruction.Create(OpCodes.Ldc_I4_2));
            il.Append(Instruction.Create(OpCodes.Stloc, endState));
            il.Append(Instruction.Create(OpCodes.Ldc_I8, unchecked((long)pointId)));
            if (mode == PerformanceInstrumentationMode.Span)
                il.Append(Instruction.Create(OpCodes.Ldc_I8, unchecked((long)metricId)));
            il.Append(Instruction.Create(OpCodes.Ldsfld, BindField(marker)));
            il.Append(Instruction.Create(OpCodes.Call, enter));
            il.Append(Instruction.Create(OpCodes.Stloc, scope));
            Instruction tryStart = Instruction.Create(OpCodes.Nop);
            il.Append(tryStart);
            if (!method.IsStatic)
                il.Append(Instruction.Create(OpCodes.Ldarg_0));
            for (int i = 0; i < method.Parameters.Count; i++)
                il.Append(Instruction.Create(OpCodes.Ldarg, method.Parameters[i]));
            il.Append(Instruction.Create(OpCodes.Call, BindMethod(original)));
            if (result != null)
                il.Append(Instruction.Create(OpCodes.Stloc, result));
            il.Append(Instruction.Create(OpCodes.Ldc_I4_1));
            il.Append(Instruction.Create(OpCodes.Stloc, endState));
            Instruction afterFinally = Instruction.Create(OpCodes.Nop);
            il.Append(Instruction.Create(OpCodes.Leave, afterFinally));
            Instruction finallyStart = Instruction.Create(OpCodes.Ldloca, scope);
            il.Append(finallyStart);
            il.Append(Instruction.Create(OpCodes.Ldloc, endState));
            il.Append(Instruction.Create(OpCodes.Call, exit));
            il.Append(Instruction.Create(OpCodes.Endfinally));
            il.Append(afterFinally);
            if (result != null)
                il.Append(Instruction.Create(OpCodes.Ldloc, result));
            il.Append(Instruction.Create(OpCodes.Ret));
            wrapperBody.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = tryStart,
                TryEnd = finallyStart,
                HandlerStart = finallyStart,
                HandlerEnd = afterFinally
            });
            method.Body = wrapperBody;
            method.DebugInformation.SequencePoints.Clear();
            method.DebugInformation.Scope = null;
        }

        static void CopySequencePoints(
            IReadOnlyList<SequencePoint> source,
            MethodDefinition target)
        {
            for (int i = 0; i < source.Count; i++)
            {
                SequencePoint sequencePoint = source[i];
                Instruction instruction = target.Body.Instructions.FirstOrDefault(
                    value => value.Offset == sequencePoint.Offset);
                if (instruction == null)
                    continue;
                target.DebugInformation.SequencePoints.Add(new SequencePoint(instruction, sequencePoint.Document)
                {
                    StartLine = sequencePoint.StartLine,
                    StartColumn = sequencePoint.StartColumn,
                    EndLine = sequencePoint.EndLine,
                    EndColumn = sequencePoint.EndColumn
                });
            }
        }

        static MethodBody CloneBody(MethodDefinition source, MethodDefinition target)
        {
            MethodBody sourceBody = source.Body;
            var targetBody = new MethodBody(target)
            {
                InitLocals = sourceBody.InitLocals,
                MaxStackSize = Math.Max(sourceBody.MaxStackSize, 8)
            };
            var variables = new Dictionary<VariableDefinition, VariableDefinition>();
            foreach (VariableDefinition variable in sourceBody.Variables)
            {
                var copy = new VariableDefinition(variable.VariableType);
                variables.Add(variable, copy);
                targetBody.Variables.Add(copy);
            }

            var instructions = new Dictionary<Instruction, Instruction>();
            foreach (Instruction instruction in sourceBody.Instructions)
            {
                Instruction copy = CreateInstruction(
                    instruction,
                    variables,
                    target);
                copy.Offset = instruction.Offset;
                instructions.Add(instruction, copy);
                targetBody.Instructions.Add(copy);
            }
            for (int i = 0; i < sourceBody.Instructions.Count; i++)
            {
                Instruction sourceInstruction = sourceBody.Instructions[i];
                Instruction targetInstruction = targetBody.Instructions[i];
                targetInstruction.Operand = CloneOperand(
                    sourceInstruction.Operand,
                    instructions,
                    variables,
                    target);
            }
            foreach (ExceptionHandler handler in sourceBody.ExceptionHandlers)
            {
                targetBody.ExceptionHandlers.Add(new ExceptionHandler(handler.HandlerType)
                {
                    CatchType = handler.CatchType,
                    TryStart = instructions[handler.TryStart],
                    TryEnd = handler.TryEnd == null ? null : instructions[handler.TryEnd],
                    FilterStart = handler.FilterStart == null ? null : instructions[handler.FilterStart],
                    HandlerStart = instructions[handler.HandlerStart],
                    HandlerEnd = handler.HandlerEnd == null ? null : instructions[handler.HandlerEnd]
                });
            }
            return targetBody;
        }

        static Instruction CreateInstruction(
            Instruction source,
            Dictionary<VariableDefinition, VariableDefinition> variables,
            MethodDefinition target)
        {
            object operand = source.Operand;
            if (operand == null)
                return Instruction.Create(source.OpCode);
            if (operand is sbyte sbyteValue)
                return Instruction.Create(source.OpCode, sbyteValue);
            if (operand is byte byteValue)
                return Instruction.Create(source.OpCode, byteValue);
            if (operand is int intValue)
                return Instruction.Create(source.OpCode, intValue);
            if (operand is long longValue)
                return Instruction.Create(source.OpCode, longValue);
            if (operand is float floatValue)
                return Instruction.Create(source.OpCode, floatValue);
            if (operand is double doubleValue)
                return Instruction.Create(source.OpCode, doubleValue);
            if (operand is string stringValue)
                return Instruction.Create(source.OpCode, stringValue);
            if (operand is Instruction)
                return Instruction.Create(source.OpCode, Instruction.Create(OpCodes.Nop));
            if (operand is Instruction[])
                return Instruction.Create(source.OpCode, Array.Empty<Instruction>());
            if (operand is VariableDefinition variable)
                return Instruction.Create(source.OpCode, variables[variable]);
            if (operand is ParameterDefinition parameter)
                return Instruction.Create(source.OpCode, target.Parameters[parameter.Index]);
            if (operand is MethodReference method)
                return Instruction.Create(source.OpCode, method);
            if (operand is FieldReference field)
                return Instruction.Create(source.OpCode, field);
            if (operand is TypeReference type)
                return Instruction.Create(source.OpCode, type);
            if (operand is CallSite callSite)
                return Instruction.Create(source.OpCode, callSite);
            throw new InvalidOperationException(
                $"Unsupported IL operand type '{operand.GetType().FullName}'.");
        }

        static object CloneOperand(
            object operand,
            Dictionary<Instruction, Instruction> instructions,
            Dictionary<VariableDefinition, VariableDefinition> variables,
            MethodDefinition target)
        {
            if (operand is Instruction instruction)
                return instructions[instruction];
            if (operand is Instruction[] instructionArray)
            {
                var copies = new Instruction[instructionArray.Length];
                for (int i = 0; i < instructionArray.Length; i++)
                    copies[i] = instructions[instructionArray[i]];
                return copies;
            }
            if (operand is VariableDefinition variable)
                return variables[variable];
            if (operand is ParameterDefinition parameter)
                return target.Parameters[parameter.Index];
            return operand;
        }

        static TypeReference CreateMarkerType(ModuleDefinition module) =>
            new TypeReference(
                PerformanceInstrumentationWeaverConstants.MarkerNamespace,
                PerformanceInstrumentationWeaverConstants.MarkerTypeName,
                module,
                GetAssemblyReference(module, "UnityEngine.CoreModule"),
                true);

        static AssemblyNameReference GetAssemblyReference(ModuleDefinition module, string name)
        {
            AssemblyNameReference reference = module.AssemblyReferences.FirstOrDefault(value => value.Name == name);
            if (reference != null)
                return reference;
            reference = new AssemblyNameReference(name, new Version(0, 0, 0, 0));
            module.AssemblyReferences.Add(reference);
            return reference;
        }

        static TypeReference BindDeclaringType(TypeDefinition type)
        {
            if (!type.HasGenericParameters)
                return type;
            var instance = new GenericInstanceType(type);
            foreach (GenericParameter parameter in type.GenericParameters)
                instance.GenericArguments.Add(parameter);
            return instance;
        }

        static FieldReference BindField(FieldDefinition field) =>
            field.DeclaringType.HasGenericParameters
                ? new FieldReference(field.Name, field.FieldType, BindDeclaringType(field.DeclaringType))
                : field;

        static MethodReference BindMethod(MethodDefinition method)
        {
            if (!method.DeclaringType.HasGenericParameters)
                return method;
            var reference = new MethodReference(method.Name, method.ReturnType, BindDeclaringType(method.DeclaringType))
            {
                HasThis = method.HasThis,
                ExplicitThis = method.ExplicitThis,
                CallingConvention = method.CallingConvention
            };
            foreach (ParameterDefinition parameter in method.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            return reference;
        }
    }
}
