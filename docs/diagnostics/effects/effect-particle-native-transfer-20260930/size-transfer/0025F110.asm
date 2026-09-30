0025F110  48895c2408                     mov       qword ptr [rsp + 8], rbx
0025F115  48896c2410                     mov       qword ptr [rsp + 0x10], rbp
0025F11A  4889742418                     mov       qword ptr [rsp + 0x18], rsi
0025F11F  57                             push      rdi
0025F120  4883ec30                       sub       rsp, 0x30
0025F124  488bf2                         mov       rsi, rdx
0025F127  488bf9                         mov       rdi, rcx
0025F12A  e8d16ffdff                     call      0x236100
0025F12F  33ed                           xor       ebp, ebp
0025F131  4c8d4f10                       lea       r9, [rdi + 0x10]
0025F135  4c8d055c5e8801                 lea       r8, [rip + 0x1885e5c] ; RIP_RVA=0x1ae4f98
0025F13C  896c2420                       mov       dword ptr [rsp + 0x20], ebp
0025F140  488d15018a8801                 lea       rdx, [rip + 0x1888a01] ; RIP_RVA=0x1ae7b48
0025F147  488bce                         mov       rcx, rsi
0025F14A  e851319700                     call      0xbd22a0
0025F14F  488bd6                         mov       rdx, rsi
0025F152  488d4f10                       lea       rcx, [rdi + 0x10]
0025F156  e87586faff                     call      0x2077d0
0025F15B  488bce                         mov       rcx, rsi
0025F15E  e86d3f9700                     call      0xbd30d0
0025F163  488d4f10                       lea       rcx, [rdi + 0x10]
0025F167  e83414fbff                     call      0x2105a0
0025F16C  4c8d4f30                       lea       r9, [rdi + 0x30]
0025F170  896c2420                       mov       dword ptr [rsp + 0x20], ebp
0025F174  4c8d051d5e8801                 lea       r8, [rip + 0x1885e1d] ; RIP_RVA=0x1ae4f98
0025F17B  488bce                         mov       rcx, rsi
0025F17E  488d15c7a68701                 lea       rdx, [rip + 0x187a6c7] ; RIP_RVA=0x1ad984c
0025F185  e816319700                     call      0xbd22a0
0025F18A  488bd6                         mov       rdx, rsi
0025F18D  488d4f30                       lea       rcx, [rdi + 0x30]
0025F191  e83a86faff                     call      0x2077d0
0025F196  488bce                         mov       rcx, rsi
0025F199  e8323f9700                     call      0xbd30d0
0025F19E  488d4f30                       lea       rcx, [rdi + 0x30]
0025F1A2  e8f913fbff                     call      0x2105a0
0025F1A7  4c8d4f50                       lea       r9, [rdi + 0x50]
0025F1AB  896c2420                       mov       dword ptr [rsp + 0x20], ebp
0025F1AF  4c8d05e25d8801                 lea       r8, [rip + 0x1885de2] ; RIP_RVA=0x1ae4f98
0025F1B6  488bce                         mov       rcx, rsi
0025F1B9  488d1590a68701                 lea       rdx, [rip + 0x187a690] ; RIP_RVA=0x1ad9850
0025F1C0  e8db309700                     call      0xbd22a0
0025F1C5  488bd6                         mov       rdx, rsi
0025F1C8  488d4f50                       lea       rcx, [rdi + 0x50]
0025F1CC  e8ff85faff                     call      0x2077d0
0025F1D1  488bce                         mov       rcx, rsi
0025F1D4  e8f73e9700                     call      0xbd30d0
0025F1D9  488d4f50                       lea       rcx, [rdi + 0x50]
0025F1DD  e8be13fbff                     call      0x2105a0
0025F1E2  4c8b0557b58d01                 mov       r8, qword ptr [rip + 0x18db557] ; RIP_RVA=0x1b3a740
0025F1E9  4c8d4f70                       lea       r9, [rdi + 0x70]
0025F1ED  488d155c898801                 lea       rdx, [rip + 0x188895c] ; RIP_RVA=0x1ae7b50
0025F1F4  896c2420                       mov       dword ptr [rsp + 0x20], ebp
0025F1F8  488bce                         mov       rcx, rsi
0025F1FB  e8a0309700                     call      0xbd22a0
0025F200  488b4658                       mov       rax, qword ptr [rsi + 0x58]
0025F204  488b5660                       mov       rdx, qword ptr [rsi + 0x60]
0025F208  48c1e205                       shl       rdx, 5
0025F20C  488b08                         mov       rcx, qword ptr [rax]
0025F20F  c7440a0c01000000               mov       dword ptr [rdx + rcx + 0xc], 1
0025F217  488bce                         mov       rcx, rsi
0025F21A  e8b13e9700                     call      0xbd30d0
0025F21F  488bce                         mov       rcx, rsi
0025F222  488b5c2440                     mov       rbx, qword ptr [rsp + 0x40]
0025F227  488b6c2448                     mov       rbp, qword ptr [rsp + 0x48]
0025F22C  488b742450                     mov       rsi, qword ptr [rsp + 0x50]
0025F231  4883c430                       add       rsp, 0x30
0025F235  5f                             pop       rdi
0025F236  e9a52d9700                     jmp       0xbd1fe0
