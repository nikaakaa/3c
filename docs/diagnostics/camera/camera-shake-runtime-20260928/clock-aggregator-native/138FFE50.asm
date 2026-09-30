138FFE50  56                             push      rsi
138FFE51  57                             push      rdi
138FFE52  4883ec48                       sub       rsp, 0x48
138FFE56  0f29742430                     movaps    xmmword ptr [rsp + 0x30], xmm6
138FFE5B  0f28f1                         movaps    xmm6, xmm1
138FFE5E  4889ce                         mov       rsi, rcx
138FFE61  803da862f9f100                 cmp       byte ptr [rip - 0xe069d58], 0 ; RIP_RVA=0x5896110
138FFE68  0f84c6010000                   je        0x13900034
138FFE6E  803dc8e5acf100                 cmp       byte ptr [rip - 0xe531a38], 0 ; RIP_RVA=0x53ce43d
138FFE75  0f85d7010000                   jne       0x13900052
138FFE7B  488b4650                       mov       rax, qword ptr [rsi + 0x50]
138FFE7F  4885c0                         test      rax, rax
138FFE82  0f84f6010000                   je        0x1390007e
138FFE88  80783900                       cmp       byte ptr [rax + 0x39], 0
138FFE8C  740c                           je        0x138ffe9a
138FFE8E  0f28742430                     movaps    xmm6, xmmword ptr [rsp + 0x30]
138FFE93  4883c448                       add       rsp, 0x48
138FFE97  5f                             pop       rdi
138FFE98  5e                             pop       rsi
138FFE99  c3                             ret       
138FFE9A  488b7e48                       mov       rdi, qword ptr [rsi + 0x48]
138FFE9E  4885ff                         test      rdi, rdi
138FFEA1  74eb                           je        0x138ffe8e
138FFEA3  488b4618                       mov       rax, qword ptr [rsi + 0x18]
138FFEA7  4885c0                         test      rax, rax
138FFEAA  0f84d3010000                   je        0x13900083
138FFEB0  488b4028                       mov       rax, qword ptr [rax + 0x28]
138FFEB4  4885c0                         test      rax, rax
138FFEB7  0f84cb010000                   je        0x13900088
138FFEBD  f74018feffffff                 test      dword ptr [rax + 0x18], 0xfffffffe
138FFEC4  0f84c3010000                   je        0x1390008d
138FFECA  80782100                       cmp       byte ptr [rax + 0x21], 0
138FFECE  74be                           je        0x138ffe8e
138FFED0  803d60caaaf100                 cmp       byte ptr [rip - 0xe5535a0], 0 ; RIP_RVA=0x53ac937
138FFED7  0f85bf010000                   jne       0x1390009c
138FFEDD  f30f104f4c                     movss     xmm1, dword ptr [rdi + 0x4c]
138FFEE2  4889f9                         mov       rcx, rdi
138FFEE5  e8465a6503                     call      0x16f55930 ; IAONCPCJKBL.JBFGDCBJIJD(float CHDMAALKNGC)
138FFEEA  488b4e18                       mov       rcx, qword ptr [rsi + 0x18]
138FFEEE  4885c9                         test      rcx, rcx
138FFEF1  0f84d0010000                   je        0x139000c7
138FFEF7  0f57d2                         xorps     xmm2, xmm2
138FFEFA  f30f5fd0                       maxss     xmm2, xmm0
138FFEFE  48c744242000000000             mov       qword ptr [rsp + 0x20], 0
138FFF07  ba01000000                     mov       edx, 1
138FFF0C  4531c9                         xor       r9d, r9d
138FFF0F  e81ca06dfb                     call      0xefd9f30
138FFF14  488b7e48                       mov       rdi, qword ptr [rsi + 0x48]
138FFF18  4885ff                         test      rdi, rdi
138FFF1B  0f84ab010000                   je        0x139000cc
138FFF21  803d0bcaaaf100                 cmp       byte ptr [rip - 0xe5535f5], 0 ; RIP_RVA=0x53ac933
138FFF28  0f85a3010000                   jne       0x139000d1
138FFF2E  807f5504                       cmp       byte ptr [rdi + 0x55], 4
138FFF32  0f85c4000000                   jne       0x138ffffc
138FFF38  488b4e18                       mov       rcx, qword ptr [rsi + 0x18]
138FFF3C  4885c9                         test      rcx, rcx
138FFF3F  0f84c0010000                   je        0x13900105
138FFF45  48c744242000000000             mov       qword ptr [rsp + 0x20], 0
138FFF4E  f30f10152640e0ee               movss     xmm2, dword ptr [rip - 0x111fbfda] ; RIP_RVA=0x2703f7c
138FFF56  ba01000000                     mov       edx, 1
138FFF5B  4531c9                         xor       r9d, r9d
138FFF5E  e8cd9f6dfb                     call      0xefd9f30
138FFF63  488b7e18                       mov       rdi, qword ptr [rsi + 0x18]
138FFF67  4885ff                         test      rdi, rdi
138FFF6A  0f849a010000                   je        0x1390010a
138FFF70  488b4728                       mov       rax, qword ptr [rdi + 0x28]
138FFF74  4885c0                         test      rax, rax
138FFF77  0f8492010000                   je        0x1390010f
138FFF7D  f74018feffffff                 test      dword ptr [rax + 0x18], 0xfffffffe
138FFF84  0f848a010000                   je        0x13900114
138FFF8A  80782100                       cmp       byte ptr [rax + 0x21], 0
138FFF8E  7438                           je        0x138fffc8
138FFF90  c6402100                       mov       byte ptr [rax + 0x21], 0
138FFF94  488b07                         mov       rax, qword ptr [rdi]
138FFF97  0fb788c2000000                 movzx     ecx, word ptr [rax + 0xc2]
138FFF9E  4c8b84c810010000               mov       r8, qword ptr [rax + rcx*8 + 0x110]
138FFFA6  4889f9                         mov       rcx, rdi
138FFFA9  ba01000000                     mov       edx, 1
138FFFAE  ff9010010000                   call      qword ptr [rax + 0x110]
138FFFB4  4889f9                         mov       rcx, rdi
138FFFB7  31d2                           xor       edx, edx
138FFFB9  e8d29e6dfb                     call      0xefd9e90
138FFFBE  4889f9                         mov       rcx, rdi
138FFFC1  31d2                           xor       edx, edx
138FFFC3  e848ab6dfb                     call      0xefdab10
138FFFC8  488b7e48                       mov       rdi, qword ptr [rsi + 0x48]
138FFFCC  488b0d752cb5f1                 mov       rcx, qword ptr [rip - 0xe4ad38b] ; RIP_RVA=0x5452c48
138FFFD3  80b9cb00000000                 cmp       byte ptr [rcx + 0xcb], 0
138FFFDA  0f8443010000                   je        0x13900123
138FFFE0  488b15e94cd5f1                 mov       rdx, qword ptr [rip - 0xe2ab317] ; RIP_RVA=0x5654cd0
138FFFE7  4889f9                         mov       rcx, rdi
138FFFEA  e881a35bfa                     call      0xdeba370
138FFFEF  48c7464800000000               mov       qword ptr [rsi + 0x48], 0
138FFFF7  e992feffff                     jmp       0x138ffe8e
138FFFFC  f30f104748                     movss     xmm0, dword ptr [rdi + 0x48]
13900001  0f57c9                         xorps     xmm1, xmm1
13900004  0f2ec8                         ucomiss   xmm1, xmm0
13900007  7714                           ja        0x1390001d
13900009  807f5600                       cmp       byte ptr [rdi + 0x56], 0
1390000D  750e                           jne       0x1390001d
1390000F  f30f104f4c                     movss     xmm1, dword ptr [rdi + 0x4c]
13900014  0f2ec8                         ucomiss   xmm1, xmm0
13900017  0f831bffffff                   jae       0x138fff38
1390001D  f30f59b6e8000000               mulss     xmm6, dword ptr [rsi + 0xe8]
13900025  f30f58774c                     addss     xmm6, dword ptr [rdi + 0x4c]
1390002A  f30f11774c                     movss     dword ptr [rdi + 0x4c], xmm6
1390002F  e95afeffff                     jmp       0x138ffe8e
13900034  b920470400                     mov       ecx, 0x44720
13900039  e8a29397ec                     call      0x2793e0
1390003E  c605cb60f9f101                 mov       byte ptr [rip - 0xe069f35], 1 ; RIP_RVA=0x5896110
13900045  803df1e3acf100                 cmp       byte ptr [rip - 0xe531c0f], 0 ; RIP_RVA=0x53ce43d
1390004C  0f8429feffff                   je        0x138ffe7b
13900052  b98d110300                     mov       ecx, 0x3118d
13900057  e804d787fc                     call      0x1017d760
1390005C  4885c0                         test      rax, rax
1390005F  0f84c8000000                   je        0x1390012d
13900065  4889c1                         mov       rcx, rax
13900068  4889f2                         mov       rdx, rsi
1390006B  0f28d6                         movaps    xmm2, xmm6
1390006E  0f28742430                     movaps    xmm6, xmmword ptr [rsp + 0x30]
13900073  4883c448                       add       rsp, 0x48
13900077  5f                             pop       rdi
13900078  5e                             pop       rsi
13900079  e9c2a488f7                     jmp       0xb18a540
1390007E  e89d901ced                     call      0xac9120
13900083  e898901ced                     call      0xac9120
13900088  e893901ced                     call      0xac9120
1390008D  e88e1697ec                     call      0x271720
13900092  4889c1                         mov       rcx, rax
13900095  31d2                           xor       edx, edx
13900097  e814901ced                     call      0xac90b0
1390009C  b987f60000                     mov       ecx, 0xf687
139000A1  e8bad687fc                     call      0x1017d760
139000A6  4885c0                         test      rax, rax
139000A9  0f8483000000                   je        0x13900132
139000AF  4889c1                         mov       rcx, rax
139000B2  4889fa                         mov       rdx, rdi
139000B5  e8f6868ef7                     call      0xb1e87b0
139000BA  488b4e18                       mov       rcx, qword ptr [rsi + 0x18]
139000BE  4885c9                         test      rcx, rcx
139000C1  0f8530feffff                   jne       0x138ffef7
139000C7  e854901ced                     call      0xac9120
139000CC  e84f901ced                     call      0xac9120
139000D1  b983f60000                     mov       ecx, 0xf683
139000D6  e885d687fc                     call      0x1017d760
139000DB  4885c0                         test      rax, rax
139000DE  7457                           je        0x13900137
139000E0  4889c1                         mov       rcx, rax
139000E3  4889fa                         mov       rdx, rdi
139000E6  e855d788f7                     call      0xb18d840
139000EB  84c0                           test      al, al
139000ED  0f8545feffff                   jne       0x138fff38
139000F3  488b7e48                       mov       rdi, qword ptr [rsi + 0x48]
139000F7  4885ff                         test      rdi, rdi
139000FA  0f851dffffff                   jne       0x1390001d
13900100  e81b901ced                     call      0xac9120
13900105  e816901ced                     call      0xac9120
1390010A  e811901ced                     call      0xac9120
1390010F  e80c901ced                     call      0xac9120
13900114  e8071697ec                     call      0x271720
13900119  4889c1                         mov       rcx, rax
1390011C  31d2                           xor       edx, edx
1390011E  e88d8f1ced                     call      0xac90b0
13900123  e8281d97ec                     call      0x271e50
13900128  e9b3feffff                     jmp       0x138fffe0
1390012D  e8ee8f1ced                     call      0xac9120
13900132  e8e98f1ced                     call      0xac9120
13900137  e8e48f1ced                     call      0xac9120
1390013C  cc                             int3      
