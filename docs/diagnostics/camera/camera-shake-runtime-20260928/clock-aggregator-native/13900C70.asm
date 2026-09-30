13900C70  55                             push      rbp
13900C71  56                             push      rsi
13900C72  57                             push      rdi
13900C73  4883ec70                       sub       rsp, 0x70
13900C77  488d6c2470                     lea       rbp, [rsp + 0x70]
13900C7C  0f2975f0                       movaps    xmmword ptr [rbp - 0x10], xmm6
13900C80  48c745e0feffffff               mov       qword ptr [rbp - 0x20], 0xfffffffffffffffe
13900C88  0f28f1                         movaps    xmm6, xmm1
13900C8B  4889cf                         mov       rdi, rcx
13900C8E  803d8154f9f100                 cmp       byte ptr [rip - 0xe06ab7f], 0 ; RIP_RVA=0x5896116
13900C95  0f84ed000000                   je        0x13900d88
13900C9B  803dbfd7acf100                 cmp       byte ptr [rip - 0xe532841], 0 ; RIP_RVA=0x53ce461
13900CA2  0f85fe000000                   jne       0x13900da6
13900CA8  803d9cd7acf100                 cmp       byte ptr [rip - 0xe532864], 0 ; RIP_RVA=0x53ce44b
13900CAF  0f8519010000                   jne       0x13900dce
13900CB5  4889f9                         mov       rcx, rdi
13900CB8  e893c4ffff                     call      0x138fd150
13900CBD  c645df00                       mov       byte ptr [rbp - 0x21], 0
13900CC1  488d55df                       lea       rdx, [rbp - 0x21]
13900CC5  4889f9                         mov       rcx, rdi
13900CC8  e8d3d2ffff                     call      0x138fdfa0
13900CCD  f30f598788000000               mulss     xmm0, dword ptr [rdi + 0x88]
13900CD5  f30f114770                     movss     dword ptr [rdi + 0x70], xmm0
13900CDA  80bf0501000000                 cmp       byte ptr [rdi + 0x105], 0
13900CE1  0fb645df                       movzx     eax, byte ptr [rbp - 0x21]
13900CE5  0f8483000000                   je        0x13900d6e
13900CEB  84c0                           test      al, al
13900CED  757f                           jne       0x13900d6e
13900CEF  488b7760                       mov       rsi, qword ptr [rdi + 0x60]
13900CF3  4885f6                         test      rsi, rsi
13900CF6  0f84f4000000                   je        0x13900df0
13900CFC  48897dd0                       mov       qword ptr [rbp - 0x30], rdi
13900D00  488b05993fd5f1                 mov       rax, qword ptr [rip - 0xe2ac067] ; RIP_RVA=0x5654ca0
13900D07  488b08                         mov       rcx, qword ptr [rax]
13900D0A  488b4168                       mov       rax, qword ptr [rcx + 0x68]
13900D0E  4883b80001000000               cmp       qword ptr [rax + 0x100], 0
13900D16  0f84d9000000                   je        0x13900df5
13900D1C  8b461c                         mov       eax, dword ptr [rsi + 0x1c]
13900D1F  488975b0                       mov       qword ptr [rbp - 0x50], rsi
13900D23  c745b800000000                 mov       dword ptr [rbp - 0x48], 0
13900D2A  8945bc                         mov       dword ptr [rbp - 0x44], eax
13900D2D  48c745c000000000               mov       qword ptr [rbp - 0x40], 0
13900D35  488b156c3fd5f1                 mov       rdx, qword ptr [rip - 0xe2ac094] ; RIP_RVA=0x5654ca8
13900D3C  488d4db0                       lea       rcx, [rbp - 0x50]
13900D40  e83ba27ff5                     call      0x90faf80
13900D45  488b55c0                       mov       rdx, qword ptr [rbp - 0x40]
13900D49  4885d2                         test      rdx, rdx
13900D4C  0f95c1                         setne     cl
13900D4F  84c8                           test      al, cl
13900D51  7415                           je        0x13900d68
13900D53  488b75d0                       mov       rsi, qword ptr [rbp - 0x30]
13900D57  4889f1                         mov       rcx, rsi
13900D5A  e861bdffff                     call      0x138fcac0
13900D5F  c6860501000000                 mov       byte ptr [rsi + 0x105], 0
13900D66  eb14                           jmp       0x13900d7c
13900D68  31c0                           xor       eax, eax
13900D6A  488b7dd0                       mov       rdi, qword ptr [rbp - 0x30]
13900D6E  888705010000                   mov       byte ptr [rdi + 0x105], al
13900D74  4889f9                         mov       rcx, rdi
13900D77  e814efffff                     call      0x138ffc90
13900D7C  0f2875f0                       movaps    xmm6, xmmword ptr [rbp - 0x10]
13900D80  4883c470                       add       rsp, 0x70
13900D84  5f                             pop       rdi
13900D85  5e                             pop       rsi
13900D86  5d                             pop       rbp
13900D87  c3                             ret       
13900D88  b926470400                     mov       ecx, 0x44726
13900D8D  e84e8697ec                     call      0x2793e0
13900D92  c6057d53f9f101                 mov       byte ptr [rip - 0xe06ac83], 1 ; RIP_RVA=0x5896116
13900D99  803dc1d6acf100                 cmp       byte ptr [rip - 0xe53293f], 0 ; RIP_RVA=0x53ce461
13900DA0  0f8402ffffff                   je        0x13900ca8
13900DA6  b9b1110300                     mov       ecx, 0x311b1
13900DAB  e8b0c987fc                     call      0x1017d760
13900DB0  4885c0                         test      rax, rax
13900DB3  744f                           je        0x13900e04
13900DB5  4889c1                         mov       rcx, rax
13900DB8  4889fa                         mov       rdx, rdi
13900DBB  0f28d6                         movaps    xmm2, xmm6
13900DBE  0f2875f0                       movaps    xmm6, xmmword ptr [rbp - 0x10]
13900DC2  4883c470                       add       rsp, 0x70
13900DC6  5f                             pop       rdi
13900DC7  5e                             pop       rsi
13900DC8  5d                             pop       rbp
13900DC9  e9729788f7                     jmp       0xb18a540
13900DCE  b99b110300                     mov       ecx, 0x3119b
13900DD3  e888c987fc                     call      0x1017d760
13900DD8  4885c0                         test      rax, rax
13900DDB  742c                           je        0x13900e09
13900DDD  4889c1                         mov       rcx, rax
13900DE0  4889fa                         mov       rdx, rdi
13900DE3  0f28d6                         movaps    xmm2, xmm6
13900DE6  e8559788f7                     call      0xb18a540
13900DEB  e9cdfeffff                     jmp       0x13900cbd
13900DF0  e82b831ced                     call      0xac9120
13900DF5  ba1e000000                     mov       edx, 0x1e
13900DFA  e851f196ec                     call      0x26ff50
13900DFF  e918ffffff                     jmp       0x13900d1c
13900E04  e817831ced                     call      0xac9120
13900E09  e812831ced                     call      0xac9120
13900E0E  48837dc800                     cmp       qword ptr [rbp - 0x38], 0
13900E13  740b                           je        0x13900e20
13900E15  488b4dc8                       mov       rcx, qword ptr [rbp - 0x38]
13900E19  31d2                           xor       edx, edx
13900E1B  e890821ced                     call      0xac90b0
13900E20  0fb645df                       movzx     eax, byte ptr [rbp - 0x21]
13900E24  e941ffffff                     jmp       0x13900d6a
