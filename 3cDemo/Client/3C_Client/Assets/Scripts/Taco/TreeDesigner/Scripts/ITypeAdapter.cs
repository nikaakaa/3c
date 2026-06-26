using System;
using System.Collections;
using System.Collections.Generic;

namespace TreeDesigner
{
    public interface ITypeAdapter
    {
        public IEnumerable<(Type, Type)> GetIncompatibleTypes();
    }
}