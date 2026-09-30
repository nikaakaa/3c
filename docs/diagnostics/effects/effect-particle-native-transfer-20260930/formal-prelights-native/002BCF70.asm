002BCF70  48895c2408                     mov       qword ptr [rsp + 8], rbx
002BCF75  48896c2410                     mov       qword ptr [rsp + 0x10], rbp
002BCF7A  4889742418                     mov       qword ptr [rsp + 0x18], rsi
002BCF7F  57                             push      rdi
002BCF80  4156                           push      r14
002BCF82  4157                           push      r15
002BCF84  4883ec60                       sub       rsp, 0x60
002BCF88  488bfa                         mov       rdi, rdx
002BCF8B  4c8bf1                         mov       r14, rcx
002BCF8E  e86d91f7ff                     call      0x236100
002BCF93  488d0556fa8201                 lea       rax, [rip + 0x182fa56] ; RIP_RVA=0x1aec9f0
002BCF9A  bd06000000                     mov       ebp, 6
002BCF9F  4889442430                     mov       qword ptr [rsp + 0x30], rax
002BCFA4  488d742430                     lea       rsi, [rsp + 0x30]
002BCFA9  488d0550fa8201                 lea       rax, [rip + 0x182fa50] ; RIP_RVA=0x1aeca00
002BCFB0  4533ff                         xor       r15d, r15d
002BCFB3  4889442438                     mov       qword ptr [rsp + 0x38], rax
002BCFB8  498d5e24                       lea       rbx, [r14 + 0x24]
002BCFBC  488d054dfa8201                 lea       rax, [rip + 0x182fa4d] ; RIP_RVA=0x1aeca10
002BCFC3  4889442440                     mov       qword ptr [rsp + 0x40], rax
002BCFC8  488d0551fa8201                 lea       rax, [rip + 0x182fa51] ; RIP_RVA=0x1aeca20
002BCFCF  4889442448                     mov       qword ptr [rsp + 0x48], rax
002BCFD4  488d0555fa8201                 lea       rax, [rip + 0x182fa55] ; RIP_RVA=0x1aeca30
002BCFDB  4889442450                     mov       qword ptr [rsp + 0x50], rax
002BCFE0  488d0559fa8201                 lea       rax, [rip + 0x182fa59] ; RIP_RVA=0x1aeca40
002BCFE7  4889442458                     mov       qword ptr [rsp + 0x58], rax
002BCFEC  0f1f4000                       nop       dword ptr [rax]
002BCFF0  488b16                         mov       rdx, qword ptr [rsi]
002BCFF3  4c8d05def48201                 lea       r8, [rip + 0x182f4de] ; RIP_RVA=0x1aec4d8
002BCFFA  4c8bcb                         mov       r9, rbx
002BCFFD  44897c2420                     mov       dword ptr [rsp + 0x20], r15d
002BD002  488bcf                         mov       rcx, rdi
002BD005  e896529100                     call      0xbd22a0
002BD00A  488bd7                         mov       rdx, rdi
002BD00D  488bcb                         mov       rcx, rbx
002BD010  e89bc4e3ff                     call      0xf94b0
002BD015  488bcf                         mov       rcx, rdi
002BD018  e8b3609100                     call      0xbd30d0
002BD01D  4883c304                       add       rbx, 4
002BD021  488d7608                       lea       rsi, [rsi + 8]
002BD025  4883ed01                       sub       rbp, 1
002BD029  75c5                           jne       0x2bcff0
002BD02B  498d4e10                       lea       rcx, [r14 + 0x10]
002BD02F  488bd7                         mov       rdx, rdi
002BD032  4c8d0517fa8201                 lea       r8, [rip + 0x182fa17] ; RIP_RVA=0x1aeca50
002BD039  e8923d0000                     call      0x2c0dd0
002BD03E  498d4e14                       lea       rcx, [r14 + 0x14]
002BD042  488bd7                         mov       rdx, rdi
002BD045  4c8d050cfa8201                 lea       r8, [rip + 0x182fa0c] ; RIP_RVA=0x1aeca58
002BD04C  e87f3d0000                     call      0x2c0dd0
002BD051  498d4e18                       lea       rcx, [r14 + 0x18]
002BD055  488bd7                         mov       rdx, rdi
002BD058  4c8d0501fa8201                 lea       r8, [rip + 0x182fa01] ; RIP_RVA=0x1aeca60
002BD05F  e86c3d0000                     call      0x2c0dd0
002BD064  498d4e1c                       lea       rcx, [r14 + 0x1c]
002BD068  488bd7                         mov       rdx, rdi
002BD06B  4c8d05f6f98201                 lea       r8, [rip + 0x182f9f6] ; RIP_RVA=0x1aeca68
002BD072  e8593d0000                     call      0x2c0dd0
002BD077  4c8b05ead68701                 mov       r8, qword ptr [rip + 0x187d6ea] ; RIP_RVA=0x1b3a768
002BD07E  4d8d4e20                       lea       r9, [r14 + 0x20]
002BD082  488d15e7d28201                 lea       rdx, [rip + 0x182d2e7] ; RIP_RVA=0x1aea370
002BD089  44897c2420                     mov       dword ptr [rsp + 0x20], r15d
002BD08E  488bcf                         mov       rcx, rdi
002BD091  e80a529100                     call      0xbd22a0
002BD096  488b4758                       mov       rax, qword ptr [rdi + 0x58]
002BD09A  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
002BD09E  48c1e205                       shl       rdx, 5
002BD0A2  488b08                         mov       rcx, qword ptr [rax]
002BD0A5  c7440a0c04000000               mov       dword ptr [rdx + rcx + 0xc], 4
002BD0AD  488bcf                         mov       rcx, rdi
002BD0B0  e81b609100                     call      0xbd30d0
002BD0B5  f3410f104e20                   movss     xmm1, dword ptr [r14 + 0x20]
002BD0BB  f30f1005191a8201               movss     xmm0, dword ptr [rip + 0x1821a19] ; RIP_RVA=0x1adeadc
002BD0C3  0f2fc1                         comiss    xmm0, xmm1
002BD0C6  770c                           ja        0x2bd0d4
002BD0C8  f30f1005c4668101               movss     xmm0, dword ptr [rip + 0x18166c4] ; RIP_RVA=0x1ad3794
002BD0D0  f30f5dc1                       minss     xmm0, xmm1
002BD0D4  4c8d5c2460                     lea       r11, [rsp + 0x60]
002BD0D9  f3410f114620                   movss     dword ptr [r14 + 0x20], xmm0
002BD0DF  498b5b20                       mov       rbx, qword ptr [r11 + 0x20]
002BD0E3  498b6b28                       mov       rbp, qword ptr [r11 + 0x28]
002BD0E7  498b7330                       mov       rsi, qword ptr [r11 + 0x30]
002BD0EB  498be3                         mov       rsp, r11
002BD0EE  415f                           pop       r15
002BD0F0  415e                           pop       r14
002BD0F2  5f                             pop       rdi
002BD0F3  c3                             ret       
