00235760  488bc4                         mov       rax, rsp
00235763  55                             push      rbp
00235764  4155                           push      r13
00235766  4156                           push      r14
00235768  488d68a1                       lea       rbp, [rax - 0x5f]
0023576C  4881ece0000000                 sub       rsp, 0xe0
00235773  4c8b05c64f9001                 mov       r8, qword ptr [rip + 0x1904fc6] ; RIP_RVA=0x1b3a740
0023577A  4c8d4908                       lea       r9, [rcx + 8]
0023577E  48895810                       mov       qword ptr [rax + 0x10], rbx
00235782  4c8be9                         mov       r13, rcx
00235785  48897018                       mov       qword ptr [rax + 0x18], rsi
00235789  4533f6                         xor       r14d, r14d
0023578C  4c8960e0                       mov       qword ptr [rax - 0x20], r12
00235790  488bf2                         mov       rsi, rdx
00235793  4c8978d8                       mov       qword ptr [rax - 0x28], r15
00235797  488d15d22c8b01                 lea       rdx, [rip + 0x18b2cd2] ; RIP_RVA=0x1ae8470
0023579E  0f2970c8                       movaps    xmmword ptr [rax - 0x38], xmm6
002357A2  488bce                         mov       rcx, rsi
002357A5  0f2978b8                       movaps    xmmword ptr [rax - 0x48], xmm7
002357A9  4489742420                     mov       dword ptr [rsp + 0x20], r14d
002357AE  e8edca9900                     call      0xbd22a0
002357B3  488b4658                       mov       rax, qword ptr [rsi + 0x58]
002357B7  488bce                         mov       rcx, rsi
002357BA  488b10                         mov       rdx, qword ptr [rax]
002357BD  488b4660                       mov       rax, qword ptr [rsi + 0x60]
002357C1  48c1e005                       shl       rax, 5
002357C5  c744100c01000000               mov       dword ptr [rax + rdx + 0xc], 1
002357CD  e8fed89900                     call      0xbd30d0
002357D2  488bce                         mov       rcx, rsi
002357D5  e806c89900                     call      0xbd1fe0
002357DA  418d5607                       lea       edx, [r14 + 7]
002357DE  488bce                         mov       rcx, rsi
002357E1  e8fadc9900                     call      0xbd34e0
002357E6  4d8d4d18                       lea       r9, [r13 + 0x18]
002357EA  4489742420                     mov       dword ptr [rsp + 0x20], r14d
002357EF  4c8d05a2f78a01                 lea       r8, [rip + 0x18af7a2] ; RIP_RVA=0x1ae4f98
002357F6  488bce                         mov       rcx, rsi
002357F9  488d1538298b01                 lea       rdx, [rip + 0x18b2938] ; RIP_RVA=0x1ae8138
00235800  e89bca9900                     call      0xbd22a0
00235805  488bd6                         mov       rdx, rsi
00235808  498d4d18                       lea       rcx, [r13 + 0x18]
0023580C  e8bf1ffdff                     call      0x2077d0
00235811  488bce                         mov       rcx, rsi
00235814  e8b7d89900                     call      0xbd30d0
00235819  498d4d18                       lea       rcx, [r13 + 0x18]
0023581D  e8be83ffff                     call      0x22dbe0
00235822  4d8d4d38                       lea       r9, [r13 + 0x38]
00235826  4489742420                     mov       dword ptr [rsp + 0x20], r14d
0023582B  4c8d0566f78a01                 lea       r8, [rip + 0x18af766] ; RIP_RVA=0x1ae4f98
00235832  488bce                         mov       rcx, rsi
00235835  488d150c298b01                 lea       rdx, [rip + 0x18b290c] ; RIP_RVA=0x1ae8148
0023583C  e85fca9900                     call      0xbd22a0
00235841  488bd6                         mov       rdx, rsi
00235844  498d4d38                       lea       rcx, [r13 + 0x38]
00235848  e8831ffdff                     call      0x2077d0
0023584D  488bce                         mov       rcx, rsi
00235850  e87bd89900                     call      0xbd30d0
00235855  498d4d38                       lea       rcx, [r13 + 0x38]
00235859  e88283ffff                     call      0x22dbe0
0023585E  4c8b050b4f9001                 mov       r8, qword ptr [rip + 0x1904f0b] ; RIP_RVA=0x1b3a770
00235865  4d8d4d58                       lea       r9, [r13 + 0x58]
00235869  488d15f0288b01                 lea       rdx, [rip + 0x18b28f0] ; RIP_RVA=0x1ae8160
00235870  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00235875  488bce                         mov       rcx, rsi
00235878  e823ca9900                     call      0xbd22a0
0023587D  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00235881  488b08                         mov       rcx, qword ptr [rax]
00235884  488b4660                       mov       rax, qword ptr [rsi + 0x60]
00235888  48c1e005                       shl       rax, 5
0023588C  c744080c04000000               mov       dword ptr [rax + rcx + 0xc], 4
00235894  488bce                         mov       rcx, rsi
00235897  e834d89900                     call      0xbd30d0
0023589C  418b4558                       mov       eax, dword ptr [r13 + 0x58]
002358A0  458d7e08                       lea       r15d, [r14 + 8]
002358A4  85c0                           test      eax, eax
002358A6  7905                           jns       0x2358ad
002358A8  418bc6                         mov       eax, r14d
002358AB  eb07                           jmp       0x2358b4
002358AD  413bc7                         cmp       eax, r15d
002358B0  410f4fc7                       cmovg     eax, r15d
002358B4  488bce                         mov       rcx, rsi
002358B7  41894558                       mov       dword ptr [r13 + 0x58], eax
002358BB  e820c79900                     call      0xbd1fe0
002358C0  4d636558                       movsxd    r12, dword ptr [r13 + 0x58]
002358C4  498d4d60                       lea       rcx, [r13 + 0x60]
002358C8  4c8b05514f9001                 mov       r8, qword ptr [rip + 0x1904f51] ; RIP_RVA=0x1b3a820
002358CF  4c8d4df7                       lea       r9, [rbp - 9]
002358D3  48894df7                       mov       qword ptr [rbp - 9], rcx
002358D7  488d1592288b01                 lea       rdx, [rip + 0x18b2892] ; RIP_RVA=0x1ae8170
002358DE  498bdc                         mov       rbx, r12
002358E1  c745ff4c000000                 mov       dword ptr [rbp - 1], 0x4c
002358E8  48c1e306                       shl       rbx, 6
002358EC  488bc3                         mov       rax, rbx
002358EF  4489742420                     mov       dword ptr [rsp + 0x20], r14d
002358F4  482bc1                         sub       rax, rcx
002358F7  488bce                         mov       rcx, rsi
002358FA  4883c060                       add       rax, 0x60
002358FE  4903c5                         add       rax, r13
00235901  48c1f806                       sar       rax, 6
00235905  48894507                       mov       qword ptr [rbp + 7], rax
00235909  4803c0                         add       rax, rax
0023590C  4883c801                       or        rax, 1
00235910  4889450f                       mov       qword ptr [rbp + 0xf], rax
00235914  e887c99900                     call      0xbd22a0
00235919  f30f103d3f3f8a01               movss     xmm7, dword ptr [rip + 0x18a3f3f] ; RIP_RVA=0x1ad9860
00235921  488d05b8fc8a01                 lea       rax, [rip + 0x18afcb8] ; RIP_RVA=0x1ae55e0
00235928  0f57c0                         xorps     xmm0, xmm0
0023592B  488945b7                       mov       qword ptr [rbp - 0x49], rax
0023592F  0f28d7                         movaps    xmm2, xmm7
00235932  c745c717000000                 mov       dword ptr [rbp - 0x39], 0x17
00235939  33d2                           xor       edx, edx
0023593B  488d4dc7                       lea       rcx, [rbp - 0x39]
0023593F  f30f7f45d7                     movdqu    xmmword ptr [rbp - 0x29], xmm0
00235944  e8078cfeff                     call      0x21e550
00235949  4533c0                         xor       r8d, r8d
0023594C  488d55b7                       lea       rdx, [rbp - 0x49]
00235950  488bce                         mov       rcx, rsi
00235953  e8b85d0000                     call      0x23b710
00235958  488b45b7                       mov       rax, qword ptr [rbp - 0x49]
0023595C  488d4db7                       lea       rcx, [rbp - 0x49]
00235960  33d2                           xor       edx, edx
00235962  ff10                           call      qword ptr [rax]
00235964  488bce                         mov       rcx, rsi
00235967  e874c69900                     call      0xbd1fe0
0023596C  488bce                         mov       rcx, rsi
0023596F  e85cd79900                     call      0xbd30d0
00235974  4d3be7                         cmp       r12, r15
00235977  734e                           jae       0x2359c7
00235979  4889bc2418010000               mov       qword ptr [rsp + 0x118], rdi
00235981  498d7d70                       lea       rdi, [r13 + 0x70]
00235985  4803fb                         add       rdi, rbx
00235988  4d2bfc                         sub       r15, r12
0023598B  4c8d254efc8a01                 lea       r12, [rip + 0x18afc4e] ; RIP_RVA=0x1ae55e0
00235992  8b1f                           mov       ebx, dword ptr [rdi]
00235994  0f28d7                         movaps    xmm2, xmm7
00235997  4c8967f0                       mov       qword ptr [rdi - 0x10], r12
0023599B  33d2                           xor       edx, edx
0023599D  488bcf                         mov       rcx, rdi
002359A0  c70717000000                   mov       dword ptr [rdi], 0x17
002359A6  4c897710                       mov       qword ptr [rdi + 0x10], r14
002359AA  4c897718                       mov       qword ptr [rdi + 0x18], r14
002359AE  e89d8bfeff                     call      0x21e550
002359B3  891f                           mov       dword ptr [rdi], ebx
002359B5  4883c740                       add       rdi, 0x40
002359B9  4983ef01                       sub       r15, 1
002359BD  75d3                           jne       0x235992
002359BF  488bbc2418010000               mov       rdi, qword ptr [rsp + 0x118]
002359C7  4c8b05a24d9001                 mov       r8, qword ptr [rip + 0x1904da2] ; RIP_RVA=0x1b3a770
002359CE  4d8d8d60020000                 lea       r9, [r13 + 0x260]
002359D5  488d15a4278b01                 lea       rdx, [rip + 0x18b27a4] ; RIP_RVA=0x1ae8180
002359DC  4489742420                     mov       dword ptr [rsp + 0x20], r14d
002359E1  488bce                         mov       rcx, rsi
002359E4  e8b7c89900                     call      0xbd22a0
002359E9  488b4658                       mov       rax, qword ptr [rsi + 0x58]
002359ED  488b08                         mov       rcx, qword ptr [rax]
002359F0  488b4660                       mov       rax, qword ptr [rsi + 0x60]
002359F4  48c1e005                       shl       rax, 5
002359F8  c744080c04000000               mov       dword ptr [rax + rcx + 0xc], 4
00235A00  488bce                         mov       rcx, rsi
00235A03  e8c8d69900                     call      0xbd30d0
00235A08  418b8560020000                 mov       eax, dword ptr [r13 + 0x260]
00235A0F  4c8bbc24d0000000               mov       r15, qword ptr [rsp + 0xd0]
00235A17  4c8ba424d8000000               mov       r12, qword ptr [rsp + 0xd8]
00235A1F  85c0                           test      eax, eax
00235A21  7905                           jns       0x235a28
00235A23  418bc6                         mov       eax, r14d
00235A26  eb0a                           jmp       0x235a32
00235A28  b980000000                     mov       ecx, 0x80
00235A2D  3bc1                           cmp       eax, ecx
00235A2F  0f4fc1                         cmovg     eax, ecx
00235A32  488bce                         mov       rcx, rsi
00235A35  41898560020000                 mov       dword ptr [r13 + 0x260], eax
00235A3C  e89fc59900                     call      0xbd1fe0
00235A41  49638560020000                 movsxd    rax, dword ptr [r13 + 0x260]
00235A48  4c897597                       mov       qword ptr [rbp - 0x69], r14
00235A4C  c7459f4c000000                 mov       dword ptr [rbp - 0x61], 0x4c
00235A53  4c8975a7                       mov       qword ptr [rbp - 0x59], r14
00235A57  4c8975af                       mov       qword ptr [rbp - 0x51], r14
00235A5B  85c0                           test      eax, eax
00235A5D  7e1a                           jle       0x235a79
00235A5F  498b8d68020000                 mov       rcx, qword ptr [r13 + 0x268]
00235A66  488945a7                       mov       qword ptr [rbp - 0x59], rax
00235A6A  4803c0                         add       rax, rax
00235A6D  4883c801                       or        rax, 1
00235A71  48894d97                       mov       qword ptr [rbp - 0x69], rcx
00235A75  488945af                       mov       qword ptr [rbp - 0x51], rax
00235A79  4c8b05a04d9001                 mov       r8, qword ptr [rip + 0x1904da0] ; RIP_RVA=0x1b3a820
00235A80  4c8d4d97                       lea       r9, [rbp - 0x69]
00235A84  488d150d278b01                 lea       rdx, [rip + 0x18b270d] ; RIP_RVA=0x1ae8198
00235A8B  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00235A90  488bce                         mov       rcx, rsi
00235A93  e808c89900                     call      0xbd22a0
00235A98  488d0549268b01                 lea       rax, [rip + 0x18b2649] ; RIP_RVA=0x1ae80e8
00235A9F  4c8975d7                       mov       qword ptr [rbp - 0x29], r14
00235AA3  488945b7                       mov       qword ptr [rbp - 0x49], rax
00235AA7  4c8d4d67                       lea       r9, [rbp + 0x67]
00235AAB  33c0                           xor       eax, eax
00235AAD  448975df                       mov       dword ptr [rbp - 0x21], r14d
00235AB1  4c8d05c42b8a01                 lea       r8, [rip + 0x18a2bc4] ; RIP_RVA=0x1ad867c
00235AB8  488945e3                       mov       qword ptr [rbp - 0x1d], rax
00235ABC  488d15b92b8a01                 lea       rdx, [rip + 0x18a2bb9] ; RIP_RVA=0x1ad867c
00235AC3  668945eb                       mov       word ptr [rbp - 0x15], ax
00235AC7  488bce                         mov       rcx, rsi
00235ACA  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00235ACF  e8acc59900                     call      0xbd2080
00235AD4  4c8d4db7                       lea       r9, [rbp - 0x49]
00235AD8  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00235ADD  4c8d0514268b01                 lea       r8, [rip + 0x18b2614] ; RIP_RVA=0x1ae80f8
00235AE4  488bce                         mov       rcx, rsi
00235AE7  488d156a2b8a01                 lea       rdx, [rip + 0x18a2b6a] ; RIP_RVA=0x1ad8658
00235AEE  e8adc79900                     call      0xbd22a0
00235AF3  488bd6                         mov       rdx, rsi
00235AF6  488d4db7                       lea       rcx, [rbp - 0x49]
00235AFA  e811040000                     call      0x235f10
00235AFF  488bce                         mov       rcx, rsi
00235B02  e8c9d59900                     call      0xbd30d0
00235B07  488bce                         mov       rcx, rsi
00235B0A  e821d59900                     call      0xbd3030
00235B0F  488b45b7                       mov       rax, qword ptr [rbp - 0x49]
00235B13  488d4db7                       lea       rcx, [rbp - 0x49]
00235B17  33d2                           xor       edx, edx
00235B19  ff10                           call      qword ptr [rax]
00235B1B  488bce                         mov       rcx, rsi
00235B1E  e8bdc49900                     call      0xbd1fe0
00235B23  488bce                         mov       rcx, rsi
00235B26  e8a5d59900                     call      0xbd30d0
00235B2B  488d4d97                       lea       rcx, [rbp - 0x69]
00235B2F  e83c69fdff                     call      0x20c470
00235B34  4c8b052d4c9001                 mov       r8, qword ptr [rip + 0x1904c2d] ; RIP_RVA=0x1b3a768
00235B3B  4d8d8d88020000                 lea       r9, [r13 + 0x288]
00235B42  488d1567268b01                 lea       rdx, [rip + 0x18b2667] ; RIP_RVA=0x1ae81b0
00235B49  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00235B4E  488bce                         mov       rcx, rsi
00235B51  e84ac79900                     call      0xbd22a0
00235B56  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00235B5A  488b5660                       mov       rdx, qword ptr [rsi + 0x60]
00235B5E  48c1e205                       shl       rdx, 5
00235B62  488b08                         mov       rcx, qword ptr [rax]
00235B65  c7440a0c04000000               mov       dword ptr [rdx + rcx + 0xc], 4
00235B6D  488bce                         mov       rcx, rsi
00235B70  e85bd59900                     call      0xbd30d0
00235B75  f3410f108d88020000             movss     xmm1, dword ptr [r13 + 0x288]
00235B7E  0f57f6                         xorps     xmm6, xmm6
00235B81  0f2ff1                         comiss    xmm6, xmm1
00235B84  7605                           jbe       0x235b8b
00235B86  0f57c0                         xorps     xmm0, xmm0
00235B89  eb07                           jmp       0x235b92
00235B8B  0f28c7                         movaps    xmm0, xmm7
00235B8E  f30f5dc1                       minss     xmm0, xmm1
00235B92  f3410f118588020000             movss     dword ptr [r13 + 0x288], xmm0
00235B9B  4d8d8d8c020000                 lea       r9, [r13 + 0x28c]
00235BA2  4c8b05bf4b9001                 mov       r8, qword ptr [rip + 0x1904bbf] ; RIP_RVA=0x1b3a768
00235BA9  488d1518268b01                 lea       rdx, [rip + 0x18b2618] ; RIP_RVA=0x1ae81c8
00235BB0  488bce                         mov       rcx, rsi
00235BB3  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00235BB8  e8e3c69900                     call      0xbd22a0
00235BBD  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00235BC1  488b5660                       mov       rdx, qword ptr [rsi + 0x60]
00235BC5  48c1e205                       shl       rdx, 5
00235BC9  488b08                         mov       rcx, qword ptr [rax]
00235BCC  c7440a0c04000000               mov       dword ptr [rdx + rcx + 0xc], 4
00235BD4  488bce                         mov       rcx, rsi
00235BD7  e8f4d49900                     call      0xbd30d0
00235BDC  f3410f108d8c020000             movss     xmm1, dword ptr [r13 + 0x28c]
00235BE5  0f2ff1                         comiss    xmm6, xmm1
00235BE8  7605                           jbe       0x235bef
00235BEA  0f57c0                         xorps     xmm0, xmm0
00235BED  eb07                           jmp       0x235bf6
00235BEF  0f28c7                         movaps    xmm0, xmm7
00235BF2  f30f5dc1                       minss     xmm0, xmm1
00235BF6  f3410f11858c020000             movss     dword ptr [r13 + 0x28c], xmm0
00235BFF  4d8d8d90020000                 lea       r9, [r13 + 0x290]
00235C06  4c8b055b4b9001                 mov       r8, qword ptr [rip + 0x1904b5b] ; RIP_RVA=0x1b3a768
00235C0D  488d15cc258b01                 lea       rdx, [rip + 0x18b25cc] ; RIP_RVA=0x1ae81e0
00235C14  488bce                         mov       rcx, rsi
00235C17  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00235C1C  e87fc69900                     call      0xbd22a0
00235C21  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00235C25  488b5660                       mov       rdx, qword ptr [rsi + 0x60]
00235C29  48c1e205                       shl       rdx, 5
00235C2D  488b08                         mov       rcx, qword ptr [rax]
00235C30  c7440a0c04000000               mov       dword ptr [rdx + rcx + 0xc], 4
00235C38  488bce                         mov       rcx, rsi
00235C3B  e890d49900                     call      0xbd30d0
00235C40  f3410f108d90020000             movss     xmm1, dword ptr [r13 + 0x290]
00235C49  0f2ff1                         comiss    xmm6, xmm1
00235C4C  7605                           jbe       0x235c53
00235C4E  0f57c0                         xorps     xmm0, xmm0
00235C51  eb07                           jmp       0x235c5a
00235C53  0f28c7                         movaps    xmm0, xmm7
00235C56  f30f5dc1                       minss     xmm0, xmm1
00235C5A  f3410f118590020000             movss     dword ptr [r13 + 0x290], xmm0
00235C63  4d8d8d94020000                 lea       r9, [r13 + 0x294]
00235C6A  4c8b05f74a9001                 mov       r8, qword ptr [rip + 0x1904af7] ; RIP_RVA=0x1b3a768
00235C71  488d1580258b01                 lea       rdx, [rip + 0x18b2580] ; RIP_RVA=0x1ae81f8
00235C78  488bce                         mov       rcx, rsi
00235C7B  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00235C80  e81bc69900                     call      0xbd22a0
00235C85  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00235C89  488b5660                       mov       rdx, qword ptr [rsi + 0x60]
00235C8D  48c1e205                       shl       rdx, 5
00235C91  488b08                         mov       rcx, qword ptr [rax]
00235C94  c7440a0c04000000               mov       dword ptr [rdx + rcx + 0xc], 4
00235C9C  488bce                         mov       rcx, rsi
00235C9F  e82cd49900                     call      0xbd30d0
00235CA4  f3410f108594020000             movss     xmm0, dword ptr [r13 + 0x294]
00235CAD  0f2ff0                         comiss    xmm6, xmm0
00235CB0  7707                           ja        0x235cb9
00235CB2  0f28f7                         movaps    xmm6, xmm7
00235CB5  f30f5df0                       minss     xmm6, xmm0
00235CB9  f3410f11b594020000             movss     dword ptr [r13 + 0x294], xmm6
00235CC2  4d8d8d98020000                 lea       r9, [r13 + 0x298]
00235CC9  4c8b05a04a9001                 mov       r8, qword ptr [rip + 0x1904aa0] ; RIP_RVA=0x1b3a770
00235CD0  488d1539258b01                 lea       rdx, [rip + 0x18b2539] ; RIP_RVA=0x1ae8210
00235CD7  488bce                         mov       rcx, rsi
00235CDA  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00235CDF  e8bcc59900                     call      0xbd22a0
00235CE4  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00235CE8  488b5660                       mov       rdx, qword ptr [rsi + 0x60]
00235CEC  48c1e205                       shl       rdx, 5
00235CF0  488b08                         mov       rcx, qword ptr [rax]
00235CF3  c7440a0c04000000               mov       dword ptr [rdx + rcx + 0xc], 4
00235CFB  488bce                         mov       rcx, rsi
00235CFE  e8cdd39900                     call      0xbd30d0
00235D03  418b8598020000                 mov       eax, dword ptr [r13 + 0x298]
00235D0A  0f28bc24b0000000               movaps    xmm7, xmmword ptr [rsp + 0xb0]
00235D12  0f28b424c0000000               movaps    xmm6, xmmword ptr [rsp + 0xc0]
00235D1A  488bb42410010000               mov       rsi, qword ptr [rsp + 0x110]
00235D22  488b9c2408010000               mov       rbx, qword ptr [rsp + 0x108]
00235D2A  85c0                           test      eax, eax
00235D2C  780d                           js        0x235d3b
00235D2E  b910270000                     mov       ecx, 0x2710
00235D33  3bc1                           cmp       eax, ecx
00235D35  0f4fc1                         cmovg     eax, ecx
00235D38  448bf0                         mov       r14d, eax
00235D3B  488d4df7                       lea       rcx, [rbp - 9]
00235D3F  4589b598020000                 mov       dword ptr [r13 + 0x298], r14d
00235D46  e855c3f6ff                     call      0x1a20a0
00235D4B  4881c4e0000000                 add       rsp, 0xe0
00235D52  415e                           pop       r14
00235D54  415d                           pop       r13
00235D56  5d                             pop       rbp
00235D57  c3                             ret       
