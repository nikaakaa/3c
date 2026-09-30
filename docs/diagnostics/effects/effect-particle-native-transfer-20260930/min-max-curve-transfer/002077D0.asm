002077D0  48895c2408                     mov       qword ptr [rsp + 8], rbx
002077D5  4889742410                     mov       qword ptr [rsp + 0x10], rsi
002077DA  48897c2418                     mov       qword ptr [rsp + 0x18], rdi
002077DF  4c89742420                     mov       qword ptr [rsp + 0x20], r14
002077E4  55                             push      rbp
002077E5  488d6c24a9                     lea       rbp, [rsp - 0x57]
002077EA  4881eca0000000                 sub       rsp, 0xa0
002077F1  488bfa                         mov       rdi, rdx
002077F4  488bf1                         mov       rsi, rcx
002077F7  488bcf                         mov       rcx, rdi
002077FA  ba02000000                     mov       edx, 2
002077FF  e8dcbc9c00                     call      0xbd34e0
00207804  4c8b05ed2f9301                 mov       r8, qword ptr [rip + 0x1932fed] ; RIP_RVA=0x1b3a7f8
0020780B  488d5e04                       lea       rbx, [rsi + 4]
0020780F  4533f6                         xor       r14d, r14d
00207812  488d1547db8d01                 lea       rdx, [rip + 0x18ddb47] ; RIP_RVA=0x1ae5360
00207819  4c8bcb                         mov       r9, rbx
0020781C  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00207821  488bcf                         mov       rcx, rdi
00207824  e877aa9c00                     call      0xbd22a0
00207829  488b4758                       mov       rax, qword ptr [rdi + 0x58]
0020782D  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
00207831  48c1e205                       shl       rdx, 5
00207835  488b08                         mov       rcx, qword ptr [rax]
00207838  c7440a0c02000000               mov       dword ptr [rdx + rcx + 0xc], 2
00207840  488bcf                         mov       rcx, rdi
00207843  e888b89c00                     call      0xbd30d0
00207848  488bcf                         mov       rcx, rdi
0020784B  e890a79c00                     call      0xbd1fe0
00207850  4c8b05112f9301                 mov       r8, qword ptr [rip + 0x1932f11] ; RIP_RVA=0x1b3a768
00207857  4c8d4e0c                       lea       r9, [rsi + 0xc]
0020785B  488d15eae98d01                 lea       rdx, [rip + 0x18de9ea] ; RIP_RVA=0x1ae624c
00207862  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00207867  488bcf                         mov       rcx, rdi
0020786A  e831aa9c00                     call      0xbd22a0
0020786F  488b4758                       mov       rax, qword ptr [rdi + 0x58]
00207873  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
00207877  48c1e205                       shl       rdx, 5
0020787B  488b08                         mov       rcx, qword ptr [rax]
0020787E  c7440a0c04000000               mov       dword ptr [rdx + rcx + 0xc], 4
00207886  488bcf                         mov       rcx, rdi
00207889  e842b89c00                     call      0xbd30d0
0020788E  4c8b05d32e9301                 mov       r8, qword ptr [rip + 0x1932ed3] ; RIP_RVA=0x1b3a768
00207895  4c8d4e08                       lea       r9, [rsi + 8]
00207899  488d15b8e98d01                 lea       rdx, [rip + 0x18de9b8] ; RIP_RVA=0x1ae6258
002078A0  4489742420                     mov       dword ptr [rsp + 0x20], r14d
002078A5  488bcf                         mov       rcx, rdi
002078A8  e8f3a99c00                     call      0xbd22a0
002078AD  488b4758                       mov       rax, qword ptr [rdi + 0x58]
002078B1  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
002078B5  48c1e205                       shl       rdx, 5
002078B9  488b08                         mov       rcx, qword ptr [rax]
002078BC  c7440a0c04000000               mov       dword ptr [rdx + rcx + 0xc], 4
002078C4  488bcf                         mov       rcx, rdi
002078C7  e804b89c00                     call      0xbd30d0
002078CC  0fb71b                         movzx     ebx, word ptr [rbx]
002078CF  8d43ff                         lea       eax, [rbx - 1]
002078D2  6683f801                       cmp       ax, 1
002078D6  0f96c0                         setbe     al
002078D9  44387729                       cmp       byte ptr [rdi + 0x29], r14b
002078DD  0f8539010000                   jne       0x207a1c
002078E3  84c0                           test      al, al
002078E5  741f                           je        0x207906
002078E7  488bce                         mov       rcx, rsi
002078EA  e8611f0100                     call      0x219850
002078EF  488bd0                         mov       rdx, rax
002078F2  4c8d057fe98d01                 lea       r8, [rip + 0x18de97f] ; RIP_RVA=0x1ae6278
002078F9  4533c9                         xor       r9d, r9d
002078FC  488bcf                         mov       rcx, rdi
002078FF  e86c8df7ff                     call      0x180670
00207904  eb76                           jmp       0x20797c
00207906  33c0                           xor       eax, eax
00207908  c745eb0000807f                 mov       dword ptr [rbp - 0x15], 0x7f800000
0020790F  4533c9                         xor       r9d, r9d
00207912  488945f3                       mov       qword ptr [rbp - 0xd], rax
00207916  4c8d055be98d01                 lea       r8, [rip + 0x18de95b] ; RIP_RVA=0x1ae6278
0020791D  488945fb                       mov       qword ptr [rbp - 5], rax
00207921  488d55e7                       lea       rdx, [rbp - 0x19]
00207925  89450b                         mov       dword ptr [rbp + 0xb], eax
00207928  488bcf                         mov       rcx, rdi
0020792B  4889450f                       mov       qword ptr [rbp + 0xf], rax
0020792F  48894517                       mov       qword ptr [rbp + 0x17], rax
00207933  448975e7                       mov       dword ptr [rbp - 0x19], r14d
00207937  448975ef                       mov       dword ptr [rbp - 0x11], r14d
0020793B  c745070000807f                 mov       dword ptr [rbp + 7], 0x7f800000
00207942  44897503                       mov       dword ptr [rbp + 3], r14d
00207946  4c89751f                       mov       qword ptr [rbp + 0x1f], r14
0020794A  c7452702000000                 mov       dword ptr [rbp + 0x27], 2
00207951  4c89752f                       mov       qword ptr [rbp + 0x2f], r14
00207955  4c897537                       mov       qword ptr [rbp + 0x37], r14
00207959  c7454302000000                 mov       dword ptr [rbp + 0x43], 2
00207960  c7453f02000000                 mov       dword ptr [rbp + 0x3f], 2
00207967  c7454704000000                 mov       dword ptr [rbp + 0x47], 4
0020796E  e8fd8cf7ff                     call      0x180670
00207973  488d4d1f                       lea       rcx, [rbp + 0x1f]
00207977  e8a4beeeff                     call      0xf3820
0020797C  6683fb02                       cmp       bx, 2
00207980  7522                           jne       0x2079a4
00207982  488bce                         mov       rcx, rsi
00207985  e8f6230100                     call      0x219d80
0020798A  488bd0                         mov       rdx, rax
0020798D  4c8d05d4e88d01                 lea       r8, [rip + 0x18de8d4] ; RIP_RVA=0x1ae6268
00207994  4533c9                         xor       r9d, r9d
00207997  488bcf                         mov       rcx, rdi
0020799A  e8d18cf7ff                     call      0x180670
0020799F  e9c5000000                     jmp       0x207a69
002079A4  33c0                           xor       eax, eax
002079A6  c745eb0000807f                 mov       dword ptr [rbp - 0x15], 0x7f800000
002079AD  4533c9                         xor       r9d, r9d
002079B0  488945f3                       mov       qword ptr [rbp - 0xd], rax
002079B4  4c8d05ade88d01                 lea       r8, [rip + 0x18de8ad] ; RIP_RVA=0x1ae6268
002079BB  488945fb                       mov       qword ptr [rbp - 5], rax
002079BF  488d55e7                       lea       rdx, [rbp - 0x19]
002079C3  89450b                         mov       dword ptr [rbp + 0xb], eax
002079C6  488bcf                         mov       rcx, rdi
002079C9  4889450f                       mov       qword ptr [rbp + 0xf], rax
002079CD  48894517                       mov       qword ptr [rbp + 0x17], rax
002079D1  448975e7                       mov       dword ptr [rbp - 0x19], r14d
002079D5  448975ef                       mov       dword ptr [rbp - 0x11], r14d
002079D9  c745070000807f                 mov       dword ptr [rbp + 7], 0x7f800000
002079E0  44897503                       mov       dword ptr [rbp + 3], r14d
002079E4  4c89751f                       mov       qword ptr [rbp + 0x1f], r14
002079E8  c7452702000000                 mov       dword ptr [rbp + 0x27], 2
002079EF  4c89752f                       mov       qword ptr [rbp + 0x2f], r14
002079F3  4c897537                       mov       qword ptr [rbp + 0x37], r14
002079F7  c7454302000000                 mov       dword ptr [rbp + 0x43], 2
002079FE  c7453f02000000                 mov       dword ptr [rbp + 0x3f], 2
00207A05  c7454704000000                 mov       dword ptr [rbp + 0x47], 4
00207A0C  e85f8cf7ff                     call      0x180670
00207A11  488d4d1f                       lea       rcx, [rbp + 0x1f]
00207A15  e806beeeff                     call      0xf3820
00207A1A  eb4d                           jmp       0x207a69
00207A1C  488d0de1b58b01                 lea       rcx, [rip + 0x18bb5e1] ; RIP_RVA=0x1ac3004
00207A23  c6452f01                       mov       byte ptr [rbp + 0x2f], 1
00207A27  48894def                       mov       qword ptr [rbp - 0x11], rcx
00207A2B  488d058ee88d01                 lea       rax, [rip + 0x18de88e] ; RIP_RVA=0x1ae62c0
00207A32  48894df7                       mov       qword ptr [rbp - 9], rcx
00207A36  48894dff                       mov       qword ptr [rbp - 1], rcx
00207A3A  48894d07                       mov       qword ptr [rbp + 7], rcx
00207A3E  488d4de7                       lea       rcx, [rbp - 0x19]
00207A42  488945e7                       mov       qword ptr [rbp - 0x19], rax
00207A46  c7450fbb010000                 mov       dword ptr [rbp + 0xf], 0x1bb
00207A4D  c74513ffffffff                 mov       dword ptr [rbp + 0x13], 0xffffffff
00207A54  48c7451701000000               mov       qword ptr [rbp + 0x17], 1
00207A5C  4489751f                       mov       dword ptr [rbp + 0x1f], r14d
00207A60  4c897527                       mov       qword ptr [rbp + 0x27], r14
00207A64  e8079c0801                     call      0x1291670
00207A69  4c8d9c24a0000000               lea       r11, [rsp + 0xa0]
00207A71  498b5b10                       mov       rbx, qword ptr [r11 + 0x10]
00207A75  498b7318                       mov       rsi, qword ptr [r11 + 0x18]
00207A79  498b7b20                       mov       rdi, qword ptr [r11 + 0x20]
00207A7D  4d8b7328                       mov       r14, qword ptr [r11 + 0x28]
00207A81  498be3                         mov       rsp, r11
00207A84  5d                             pop       rbp
00207A85  c3                             ret       
