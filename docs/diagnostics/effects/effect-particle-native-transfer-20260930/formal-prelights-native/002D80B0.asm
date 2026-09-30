002D80B0  48895c2408                     mov       qword ptr [rsp + 8], rbx
002D80B5  48896c2410                     mov       qword ptr [rsp + 0x10], rbp
002D80BA  4889742418                     mov       qword ptr [rsp + 0x18], rsi
002D80BF  57                             push      rdi
002D80C0  4883ec40                       sub       rsp, 0x40
002D80C4  0f29742430                     movaps    xmmword ptr [rsp + 0x30], xmm6
002D80C9  488bfa                         mov       rdi, rdx
002D80CC  488bf1                         mov       rsi, rcx
002D80CF  e82ce0f5ff                     call      0x236100
002D80D4  4c8b058d268601                 mov       r8, qword ptr [rip + 0x186268d] ; RIP_RVA=0x1b3a768
002D80DB  4c8d4e14                       lea       r9, [rsi + 0x14]
002D80DF  33ed                           xor       ebp, ebp
002D80E1  488d15ac478101                 lea       rdx, [rip + 0x18147ac] ; RIP_RVA=0x1aec894
002D80E8  488bcf                         mov       rcx, rdi
002D80EB  896c2420                       mov       dword ptr [rsp + 0x20], ebp
002D80EF  e8aca18f00                     call      0xbd22a0
002D80F4  488b4758                       mov       rax, qword ptr [rdi + 0x58]
002D80F8  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
002D80FC  48c1e205                       shl       rdx, 5
002D8100  488b08                         mov       rcx, qword ptr [rax]
002D8103  c7440a0c04000000               mov       dword ptr [rdx + rcx + 0xc], 4
002D810B  488bcf                         mov       rcx, rdi
002D810E  e8bdaf8f00                     call      0xbd30d0
002D8113  f30f104e14                     movss     xmm1, dword ptr [rsi + 0x14]
002D8118  0f57f6                         xorps     xmm6, xmm6
002D811B  0f2ff1                         comiss    xmm6, xmm1
002D811E  7605                           jbe       0x2d8125
002D8120  0f57c0                         xorps     xmm0, xmm0
002D8123  eb0c                           jmp       0x2d8131
002D8125  f30f100533178001               movss     xmm0, dword ptr [rip + 0x1801733] ; RIP_RVA=0x1ad9860
002D812D  f30f5dc1                       minss     xmm0, xmm1
002D8131  f30f114614                     movss     dword ptr [rsi + 0x14], xmm0
002D8136  4c8d4e18                       lea       r9, [rsi + 0x18]
002D813A  4c8b0527268601                 mov       r8, qword ptr [rip + 0x1862627] ; RIP_RVA=0x1b3a768
002D8141  488d15a0548101                 lea       rdx, [rip + 0x18154a0] ; RIP_RVA=0x1aed5e8
002D8148  488bcf                         mov       rcx, rdi
002D814B  896c2420                       mov       dword ptr [rsp + 0x20], ebp
002D814F  e84ca18f00                     call      0xbd22a0
002D8154  488b4758                       mov       rax, qword ptr [rdi + 0x58]
002D8158  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
002D815C  48c1e205                       shl       rdx, 5
002D8160  488b08                         mov       rcx, qword ptr [rax]
002D8163  c7440a0c04000000               mov       dword ptr [rdx + rcx + 0xc], 4
002D816B  488bcf                         mov       rcx, rdi
002D816E  e85daf8f00                     call      0xbd30d0
002D8173  f30f104618                     movss     xmm0, dword ptr [rsi + 0x18]
002D8178  0f2ff0                         comiss    xmm6, xmm0
002D817B  770c                           ja        0x2d8189
002D817D  f30f1035bbfe7f01               movss     xmm6, dword ptr [rip + 0x17ffebb] ; RIP_RVA=0x1ad8040
002D8185  f30f5df0                       minss     xmm6, xmm0
002D8189  4c8d4e10                       lea       r9, [rsi + 0x10]
002D818D  f30f117618                     movss     dword ptr [rsi + 0x18], xmm6
002D8192  4c8d05b7538101                 lea       r8, [rip + 0x18153b7] ; RIP_RVA=0x1aed550
002D8199  896c2420                       mov       dword ptr [rsp + 0x20], ebp
002D819D  488d1558548101                 lea       rdx, [rip + 0x1815458] ; RIP_RVA=0x1aed5fc
002D81A4  488bcf                         mov       rcx, rdi
002D81A7  e8f4a08f00                     call      0xbd22a0
002D81AC  488bd7                         mov       rdx, rdi
002D81AF  488d4e10                       lea       rcx, [rsi + 0x10]
002D81B3  e8f812e2ff                     call      0xf94b0
002D81B8  488bcf                         mov       rcx, rdi
002D81BB  e810af8f00                     call      0xbd30d0
002D81C0  4c8b0579258601                 mov       r8, qword ptr [rip + 0x1862579] ; RIP_RVA=0x1b3a740
002D81C7  4c8d4e1c                       lea       r9, [rsi + 0x1c]
002D81CB  488d1536548101                 lea       rdx, [rip + 0x1815436] ; RIP_RVA=0x1aed608
002D81D2  896c2420                       mov       dword ptr [rsp + 0x20], ebp
002D81D6  488bcf                         mov       rcx, rdi
002D81D9  e8c2a08f00                     call      0xbd22a0
002D81DE  488b4758                       mov       rax, qword ptr [rdi + 0x58]
002D81E2  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
002D81E6  48c1e205                       shl       rdx, 5
002D81EA  488b08                         mov       rcx, qword ptr [rax]
002D81ED  c7440a0c01000000               mov       dword ptr [rdx + rcx + 0xc], 1
002D81F5  488bcf                         mov       rcx, rdi
002D81F8  e8d3ae8f00                     call      0xbd30d0
002D81FD  4c8b053c258601                 mov       r8, qword ptr [rip + 0x186253c] ; RIP_RVA=0x1b3a740
002D8204  4c8d4e1d                       lea       r9, [rsi + 0x1d]
002D8208  488d150d548101                 lea       rdx, [rip + 0x181540d] ; RIP_RVA=0x1aed61c
002D820F  896c2420                       mov       dword ptr [rsp + 0x20], ebp
002D8213  488bcf                         mov       rcx, rdi
002D8216  e885a08f00                     call      0xbd22a0
002D821B  488b4758                       mov       rax, qword ptr [rdi + 0x58]
002D821F  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
002D8223  48c1e205                       shl       rdx, 5
002D8227  488b08                         mov       rcx, qword ptr [rax]
002D822A  c7440a0c01000000               mov       dword ptr [rdx + rcx + 0xc], 1
002D8232  488bcf                         mov       rcx, rdi
002D8235  e896ae8f00                     call      0xbd30d0
002D823A  4c8b05ff248601                 mov       r8, qword ptr [rip + 0x18624ff] ; RIP_RVA=0x1b3a740
002D8241  4c8d4e1e                       lea       r9, [rsi + 0x1e]
002D8245  488d1514f98001                 lea       rdx, [rip + 0x180f914] ; RIP_RVA=0x1ae7b60
002D824C  896c2420                       mov       dword ptr [rsp + 0x20], ebp
002D8250  488bcf                         mov       rcx, rdi
002D8253  e848a08f00                     call      0xbd22a0
002D8258  488b4758                       mov       rax, qword ptr [rdi + 0x58]
002D825C  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
002D8260  48c1e205                       shl       rdx, 5
002D8264  488b08                         mov       rcx, qword ptr [rax]
002D8267  c7440a0c01000000               mov       dword ptr [rdx + rcx + 0xc], 1
002D826F  488bcf                         mov       rcx, rdi
002D8272  e859ae8f00                     call      0xbd30d0
002D8277  4c8b05c2248601                 mov       r8, qword ptr [rip + 0x18624c2] ; RIP_RVA=0x1b3a740
002D827E  4c8d4e1f                       lea       r9, [rsi + 0x1f]
002D8282  488d159f538101                 lea       rdx, [rip + 0x181539f] ; RIP_RVA=0x1aed628
002D8289  896c2420                       mov       dword ptr [rsp + 0x20], ebp
002D828D  488bcf                         mov       rcx, rdi
002D8290  e80ba08f00                     call      0xbd22a0
002D8295  488b4758                       mov       rax, qword ptr [rdi + 0x58]
002D8299  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
002D829D  48c1e205                       shl       rdx, 5
002D82A1  488b08                         mov       rcx, qword ptr [rax]
002D82A4  c7440a0c01000000               mov       dword ptr [rdx + rcx + 0xc], 1
002D82AC  488bcf                         mov       rcx, rdi
002D82AF  e81cae8f00                     call      0xbd30d0
002D82B4  488d4e20                       lea       rcx, [rsi + 0x20]
002D82B8  488bd7                         mov       rdx, rdi
002D82BB  4c8d0576538101                 lea       r8, [rip + 0x1815376] ; RIP_RVA=0x1aed638
002D82C2  e859d3f5ff                     call      0x235620
002D82C7  488d4e40                       lea       rcx, [rsi + 0x40]
002D82CB  488bd7                         mov       rdx, rdi
002D82CE  4c8d0573538101                 lea       r8, [rip + 0x1815373] ; RIP_RVA=0x1aed648
002D82D5  e846d3f5ff                     call      0x235620
002D82DA  4c8b058f248601                 mov       r8, qword ptr [rip + 0x186248f] ; RIP_RVA=0x1b3a770
002D82E1  4c8d4e60                       lea       r9, [rsi + 0x60]
002D82E5  488d156c538101                 lea       rdx, [rip + 0x181536c] ; RIP_RVA=0x1aed658
002D82EC  896c2420                       mov       dword ptr [rsp + 0x20], ebp
002D82F0  488bcf                         mov       rcx, rdi
002D82F3  e8a89f8f00                     call      0xbd22a0
002D82F8  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
002D82FC  488b4758                       mov       rax, qword ptr [rdi + 0x58]
002D8300  48c1e205                       shl       rdx, 5
002D8304  488b08                         mov       rcx, qword ptr [rax]
002D8307  c7440a0c04000000               mov       dword ptr [rdx + rcx + 0xc], 4
002D830F  488bcf                         mov       rcx, rdi
002D8312  e8b9ad8f00                     call      0xbd30d0
002D8317  8b4660                         mov       eax, dword ptr [rsi + 0x60]
002D831A  85c0                           test      eax, eax
002D831C  488b5c2450                     mov       rbx, qword ptr [rsp + 0x50]
002D8321  0f28742430                     movaps    xmm6, xmmword ptr [rsp + 0x30]
002D8326  0f49e8                         cmovns    ebp, eax
002D8329  896e60                         mov       dword ptr [rsi + 0x60], ebp
002D832C  488b6c2458                     mov       rbp, qword ptr [rsp + 0x58]
002D8331  488b742460                     mov       rsi, qword ptr [rsp + 0x60]
002D8336  4883c440                       add       rsp, 0x40
002D833A  5f                             pop       rdi
002D833B  c3                             ret       
