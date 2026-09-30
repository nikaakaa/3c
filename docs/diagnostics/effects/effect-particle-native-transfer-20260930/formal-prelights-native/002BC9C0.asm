002BC9C0  48895c2408                     mov       qword ptr [rsp + 8], rbx
002BC9C5  57                             push      rdi
002BC9C6  4883ec20                       sub       rsp, 0x20
002BC9CA  488bfa                         mov       rdi, rdx
002BC9CD  488bd9                         mov       rbx, rcx
002BC9D0  488bcf                         mov       rcx, rdi
002BC9D3  ba02000000                     mov       edx, 2
002BC9D8  e8036b9100                     call      0xbd34e0
002BC9DD  488bd7                         mov       rdx, rdi
002BC9E0  488bcb                         mov       rcx, rbx
002BC9E3  e81897f7ff                     call      0x236100
002BC9E8  488d5310                       lea       rdx, [rbx + 0x10]
002BC9EC  4533c9                         xor       r9d, r9d
002BC9EF  4c8d0512fe8201                 lea       r8, [rip + 0x182fe12] ; RIP_RVA=0x1aec808
002BC9F6  488bcf                         mov       rcx, rdi
002BC9F9  488b5c2430                     mov       rbx, qword ptr [rsp + 0x30]
002BC9FE  4883c420                       add       rsp, 0x20
002BCA02  5f                             pop       rdi
002BCA03  e9f8f2ffff                     jmp       0x2bbd00
