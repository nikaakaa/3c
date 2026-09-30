import json, struct, ctypes, base64, os, sys

def decompress_lz4(data, size):
    result = bytearray(); cursor = 0
    while cursor < len(data):
        token = data[cursor]; cursor += 1
        literal = token >> 4
        if literal == 15:
            while True:
                extra = data[cursor]; cursor += 1; literal += extra
                if extra != 255: break
        result.extend(data[cursor:cursor+literal]); cursor += literal
        if cursor == len(data): break
        offset = struct.unpack_from("<H", data, cursor)[0]; cursor += 2
        length = (token & 15) + 4
        if (token & 15) == 15:
            while True:
                extra = data[cursor]; cursor += 1; length += extra
                if extra != 255: break
        if offset == 0 or offset > len(result): raise ValueError("Bad LZ4")
        block = bytes(result[-offset:])
        result.extend((block * ((length+offset-1)//offset))[:length])
    if cursor != len(data) or len(result) != size: raise ValueError(f"LZ4 mismatch {len(result)}!={size}")
    return bytes(result)

class Reader:
    def __init__(self, data): self.data = data; self.cursor = 0
    def i32(self): v = struct.unpack_from("<i",self.data,self.cursor)[0]; self.cursor+=4; return v
    def align4(self): self.cursor = (self.cursor+3)&~3
    def strings(self):
        c = self.i32()
        if c < 0 or c > 1024: raise ValueError(f"Bad count {c}")
        vals = []
        for _ in range(c):
            l = self.i32(); vals.append(self.data[self.cursor:self.cursor+l].decode("utf-8")); self.cursor+=l; self.align4()
        return vals

def read_program(data):
    r = Reader(data); version=r.i32(); ptype=r.i32()
    r.cursor = 24 if version >= 201608170 else 20
    kw = r.strings()
    if 201806140 <= version < 202012090: r.strings()
    length = r.i32()
    return {"type":ptype,"kw":kw}, data[r.cursor:r.cursor+length]

def get_dxbc(code):
    start = {0:1,1:6,2:38}[code[0]]
    dxbc = code[start:]
    if dxbc[:4]!=b"DXBC" or struct.unpack_from("<I",dxbc,24)[0]!=len(dxbc): raise ValueError("DXBC mismatch")
    return dxbc

d3dcompiler = ctypes.windll.LoadLibrary("C:/Windows/System32/d3dcompiler_47.dll")

def disassemble_dxbc(dxbc_data):
    blob = ctypes.c_void_p()
    hr = d3dcompiler.D3DDisassemble(
        ctypes.create_string_buffer(dxbc_data), ctypes.c_size_t(len(dxbc_data)),
        ctypes.c_uint(0), None, ctypes.byref(blob))
    if hr != 0: raise RuntimeError(f"HRESULT=0x{hr & 0xFFFFFFFF:08X}")
    vtbl = ctypes.c_void_p.from_address(blob.value).value
    GetBufferPointer = ctypes.WINFUNCTYPE(ctypes.c_void_p, ctypes.c_void_p)(ctypes.c_void_p.from_address(vtbl+3*8).value)
    GetBufferSize = ctypes.WINFUNCTYPE(ctypes.c_size_t, ctypes.c_void_p)(ctypes.c_void_p.from_address(vtbl+4*8).value)
    Release = ctypes.WINFUNCTYPE(ctypes.c_ulong, ctypes.c_void_p)(ctypes.c_void_p.from_address(vtbl+2*8).value)
    ptr, size = GetBufferPointer(blob), GetBufferSize(blob)
    result = ctypes.string_at(ptr, size).decode("utf-8", errors="replace")
    Release(blob)
    return result

def process_shader(json_path, output_dir):
    with open(json_path, "r") as f:
        shader_data = json.load(f)
    blob = base64.b64decode(shader_data["compressedBlob"])
    offsets = shader_data["offsets"][0]
    cl = shader_data["compressedLengths"][0]
    dl = shader_data["decompressedLengths"][0]
    segments = [decompress_lz4(blob[offsets[i]:offsets[i]+cl[i]], dl[i]) for i in range(len(offsets))]
    count = struct.unpack_from("<I", segments[0])[0]
    os.makedirs(output_dir, exist_ok=True)
    ok, fail = 0, 0
    for i in range(count):
        off, length, seg_idx = struct.unpack_from("<iii", segments[0], 4+i*12)
        info, code = read_program(segments[seg_idx][off:off+length])
        try:
            dxbc = get_dxbc(code)
            asm = disassemble_dxbc(dxbc)
            out = os.path.join(output_dir, f"e{i}_t{info['type']}.asm")
            with open(out, "w", encoding="utf-8") as f: f.write(asm)
            ok += 1
        except Exception:
            fail += 1
        if (i+1) % 500 == 0:
            print(f"  {i+1}/{count} ok={ok} fail={fail}", flush=True)
    print(f"Done: {ok}/{count} disassembled, {fail} failed")
    return ok

if __name__ == "__main__":
    json_path = sys.argv[1]
    output_dir = sys.argv[2]
    process_shader(json_path, output_dir)
