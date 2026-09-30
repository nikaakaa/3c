00236970  48895c2408                     mov       qword ptr [rsp + 8], rbx
00236975  48896c2410                     mov       qword ptr [rsp + 0x10], rbp
0023697A  4889742418                     mov       qword ptr [rsp + 0x18], rsi
0023697F  57                             push      rdi
00236980  4883ec30                       sub       rsp, 0x30
00236984  4c8b05b53d9001                 mov       r8, qword ptr [rip + 0x1903db5] ; RIP_RVA=0x1b3a740
0023698B  4c8d4908                       lea       r9, [rcx + 8]
0023698F  488bfa                         mov       rdi, rdx
00236992  488bf1                         mov       rsi, rcx
00236995  33ed                           xor       ebp, ebp
00236997  488d15d21a8b01                 lea       rdx, [rip + 0x18b1ad2] ; RIP_RVA=0x1ae8470
0023699E  488bcf                         mov       rcx, rdi
002369A1  896c2420                       mov       dword ptr [rsp + 0x20], ebp
002369A5  e8f6b89900                     call      0xbd22a0
002369AA  488b4758                       mov       rax, qword ptr [rdi + 0x58]
002369AE  488bcf                         mov       rcx, rdi
002369B1  4c8b4760                       mov       r8, qword ptr [rdi + 0x60]
002369B5  49c1e005                       shl       r8, 5
002369B9  488b10                         mov       rdx, qword ptr [rax]
002369BC  41c744100c01000000             mov       dword ptr [r8 + rdx + 0xc], 1
002369C5  e806c79900                     call      0xbd30d0
002369CA  488bcf                         mov       rcx, rdi
002369CD  e80eb69900                     call      0xbd1fe0
002369D2  4c8d4e10                       lea       r9, [rsi + 0x10]
002369D6  896c2420                       mov       dword ptr [rsp + 0x20], ebp
002369DA  4c8d05b7e58a01                 lea       r8, [rip + 0x18ae5b7] ; RIP_RVA=0x1ae4f98
002369E1  488bcf                         mov       rcx, rdi
002369E4  488d155d118b01                 lea       rdx, [rip + 0x18b115d] ; RIP_RVA=0x1ae7b48
002369EB  e8b0b89900                     call      0xbd22a0
002369F0  488bd7                         mov       rdx, rdi
002369F3  488d4e10                       lea       rcx, [rsi + 0x10]
002369F7  e8d40dfdff                     call      0x2077d0
002369FC  488bcf                         mov       rcx, rdi
002369FF  e8ccc69900                     call      0xbd30d0
00236A04  488d4e10                       lea       rcx, [rsi + 0x10]
00236A08  e8939bfdff                     call      0x2105a0
00236A0D  4c8d4e30                       lea       r9, [rsi + 0x30]
00236A11  896c2420                       mov       dword ptr [rsp + 0x20], ebp
00236A15  4c8d057ce58a01                 lea       r8, [rip + 0x18ae57c] ; RIP_RVA=0x1ae4f98
00236A1C  488bcf                         mov       rcx, rdi
00236A1F  488d15262e8a01                 lea       rdx, [rip + 0x18a2e26] ; RIP_RVA=0x1ad984c
00236A26  e875b89900                     call      0xbd22a0
00236A2B  488bd7                         mov       rdx, rdi
00236A2E  488d4e30                       lea       rcx, [rsi + 0x30]
00236A32  e8990dfdff                     call      0x2077d0
00236A37  488bcf                         mov       rcx, rdi
00236A3A  e891c69900                     call      0xbd30d0
00236A3F  488d4e30                       lea       rcx, [rsi + 0x30]
00236A43  e8589bfdff                     call      0x2105a0
00236A48  4c8d4e50                       lea       r9, [rsi + 0x50]
00236A4C  896c2420                       mov       dword ptr [rsp + 0x20], ebp
00236A50  4c8d0541e58a01                 lea       r8, [rip + 0x18ae541] ; RIP_RVA=0x1ae4f98
00236A57  488bcf                         mov       rcx, rdi
00236A5A  488d15ef2d8a01                 lea       rdx, [rip + 0x18a2def] ; RIP_RVA=0x1ad9850
00236A61  e83ab89900                     call      0xbd22a0
00236A66  488bd7                         mov       rdx, rdi
00236A69  488d4e50                       lea       rcx, [rsi + 0x50]
00236A6D  e85e0dfdff                     call      0x2077d0
00236A72  488bcf                         mov       rcx, rdi
00236A75  e856c69900                     call      0xbd30d0
00236A7A  488d4e50                       lea       rcx, [rsi + 0x50]
00236A7E  e81d9bfdff                     call      0x2105a0
00236A83  4533c9                         xor       r9d, r9d
00236A86  4c8d05d3108b01                 lea       r8, [rip + 0x18b10d3] ; RIP_RVA=0x1ae7b60
00236A8D  488d5670                       lea       rdx, [rsi + 0x70]
00236A91  488bcf                         mov       rcx, rdi
00236A94  e827f9f4ff                     call      0x1863c0
00236A99  f30f105674                     movss     xmm2, dword ptr [rsi + 0x74]
00236A9E  0f57c0                         xorps     xmm0, xmm0
00236AA1  f30f5f4670                     maxss     xmm0, dword ptr [rsi + 0x70]
00236AA6  0f57c9                         xorps     xmm1, xmm1
00236AA9  0f2fca                         comiss    xmm1, xmm2
00236AAC  f30f114670                     movss     dword ptr [rsi + 0x70], xmm0
00236AB1  7703                           ja        0x236ab6
00236AB3  0f28ca                         movaps    xmm1, xmm2
00236AB6  f30f114e74                     movss     dword ptr [rsi + 0x74], xmm1
00236ABB  4c8d4e78                       lea       r9, [rsi + 0x78]
00236ABF  4c8b057a3c9001                 mov       r8, qword ptr [rip + 0x1903c7a] ; RIP_RVA=0x1b3a740
00236AC6  488d1583108b01                 lea       rdx, [rip + 0x18b1083] ; RIP_RVA=0x1ae7b50
00236ACD  488bcf                         mov       rcx, rdi
00236AD0  896c2420                       mov       dword ptr [rsp + 0x20], ebp
00236AD4  e8c7b79900                     call      0xbd22a0
00236AD9  488b4758                       mov       rax, qword ptr [rdi + 0x58]
00236ADD  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
00236AE1  48c1e205                       shl       rdx, 5
00236AE5  488b08                         mov       rcx, qword ptr [rax]
00236AE8  c7440a0c01000000               mov       dword ptr [rdx + rcx + 0xc], 1
00236AF0  488bcf                         mov       rcx, rdi
00236AF3  e8d8c59900                     call      0xbd30d0
00236AF8  488bcf                         mov       rcx, rdi
00236AFB  488b5c2440                     mov       rbx, qword ptr [rsp + 0x40]
00236B00  488b6c2448                     mov       rbp, qword ptr [rsp + 0x48]
00236B05  488b742450                     mov       rsi, qword ptr [rsp + 0x50]
00236B0A  4883c430                       add       rsp, 0x30
00236B0E  5f                             pop       rdi
00236B0F  e9ccb49900                     jmp       0xbd1fe0
