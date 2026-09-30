13900140  56                             push      rsi
13900141  57                             push      rdi
13900142  53                             push      rbx
13900143  4883ec30                       sub       rsp, 0x30
13900147  89d3                           mov       ebx, edx
13900149  4889ce                         mov       rsi, rcx
1390014C  803debe2acf100                 cmp       byte ptr [rip - 0xe531d15], 0 ; RIP_RVA=0x53ce43e
13900153  7572                           jne       0x139001c7
13900155  4889f1                         mov       rcx, rsi
13900158  89da                           mov       edx, ebx
1390015A  e841020000                     call      0x139003a0
1390015F  488b4e18                       mov       rcx, qword ptr [rsi + 0x18]
13900163  4885c9                         test      rcx, rcx
13900166  0f8483000000                   je        0x139001ef
1390016C  488b4128                       mov       rax, qword ptr [rcx + 0x28]
13900170  4885c0                         test      rax, rax
13900173  747f                           je        0x139001f4
13900175  f74018fcffffff                 test      dword ptr [rax + 0x18], 0xfffffffc
1390017C  747b                           je        0x139001f9
1390017E  0fb65823                       movzx     ebx, byte ptr [rax + 0x23]
13900182  803dc6e2acf100                 cmp       byte ptr [rip - 0xe531d3a], 0 ; RIP_RVA=0x53ce44f
13900189  757d                           jne       0x13900208
1390018B  83bef000000000                 cmp       dword ptr [rsi + 0xf0], 0
13900192  0f8e3f010000                   jle       0x139002d7
13900198  84db                           test      bl, bl
1390019A  0f849a000000                   je        0x1390023a
139001A0  4885c9                         test      rcx, rcx
139001A3  0f84c5010000                   je        0x1390036e
139001A9  48c744242000000000             mov       qword ptr [rsp + 0x20], 0
139001B2  0f57d2                         xorps     xmm2, xmm2
139001B5  ba03000000                     mov       edx, 3
139001BA  4531c9                         xor       r9d, r9d
139001BD  e86e9d6dfb                     call      0xefd9f30
139001C2  e995000000                     jmp       0x1390025c
139001C7  b98e110300                     mov       ecx, 0x3118e
139001CC  e88fd587fc                     call      0x1017d760
139001D1  4885c0                         test      rax, rax
139001D4  0f84b7010000                   je        0x13900391
139001DA  4889c1                         mov       rcx, rax
139001DD  4889f2                         mov       rdx, rsi
139001E0  4189d8                         mov       r8d, ebx
139001E3  4883c430                       add       rsp, 0x30
139001E7  5b                             pop       rbx
139001E8  5f                             pop       rdi
139001E9  5e                             pop       rsi
139001EA  e921ef88f7                     jmp       0xb18f110
139001EF  e82c8f1ced                     call      0xac9120
139001F4  e8278f1ced                     call      0xac9120
139001F9  e8221597ec                     call      0x271720
139001FE  4889c1                         mov       rcx, rax
13900201  31d2                           xor       edx, edx
13900203  e8a88e1ced                     call      0xac90b0
13900208  b99f110300                     mov       ecx, 0x3119f
1390020D  e84ed587fc                     call      0x1017d760
13900212  4885c0                         test      rax, rax
13900215  0f847b010000                   je        0x13900396
1390021B  4889c1                         mov       rcx, rax
1390021E  4889f2                         mov       rdx, rsi
13900221  e81ad688f7                     call      0xb18d840
13900226  84c0                           test      al, al
13900228  0f84a9000000                   je        0x139002d7
1390022E  488b4e18                       mov       rcx, qword ptr [rsi + 0x18]
13900232  84db                           test      bl, bl
13900234  0f8566ffffff                   jne       0x139001a0
1390023A  4885c9                         test      rcx, rcx
1390023D  0f8435010000                   je        0x13900378
13900243  48c744242000000000             mov       qword ptr [rsp + 0x20], 0
1390024C  0f57d2                         xorps     xmm2, xmm2
1390024F  ba03000000                     mov       edx, 3
13900254  4531c9                         xor       r9d, r9d
13900257  e834a16dfb                     call      0xefda390
1390025C  0f57c9                         xorps     xmm1, xmm1
1390025F  4889f1                         mov       rcx, rsi
13900262  e829b7ffff                     call      0x138fb990
13900267  488b7618                       mov       rsi, qword ptr [rsi + 0x18]
1390026B  4885f6                         test      rsi, rsi
1390026E  0f84e1000000                   je        0x13900355
13900274  488b4628                       mov       rax, qword ptr [rsi + 0x28]
13900278  4885c0                         test      rax, rax
1390027B  0f84d9000000                   je        0x1390035a
13900281  f74018fcffffff                 test      dword ptr [rax + 0x18], 0xfffffffc
13900288  0f84d1000000                   je        0x1390035f
1390028E  80782300                       cmp       byte ptr [rax + 0x23], 0
13900292  0f84b4000000                   je        0x1390034c
13900298  c6402300                       mov       byte ptr [rax + 0x23], 0
1390029C  488b06                         mov       rax, qword ptr [rsi]
1390029F  0fb788c2000000                 movzx     ecx, word ptr [rax + 0xc2]
139002A6  4c8b84c810010000               mov       r8, qword ptr [rax + rcx*8 + 0x110]
139002AE  4889f1                         mov       rcx, rsi
139002B1  ba03000000                     mov       edx, 3
139002B6  ff9010010000                   call      qword ptr [rax + 0x110]
139002BC  4889f1                         mov       rcx, rsi
139002BF  31d2                           xor       edx, edx
139002C1  e8ca9b6dfb                     call      0xefd9e90
139002C6  4889f1                         mov       rcx, rsi
139002C9  31d2                           xor       edx, edx
139002CB  4883c430                       add       rsp, 0x30
139002CF  5b                             pop       rbx
139002D0  5f                             pop       rdi
139002D1  5e                             pop       rsi
139002D2  e939a86dfb                     jmp       0xefdab10
139002D7  84db                           test      bl, bl
139002D9  745f                           je        0x1390033a
139002DB  488b7e18                       mov       rdi, qword ptr [rsi + 0x18]
139002DF  4885ff                         test      rdi, rdi
139002E2  0f848b000000                   je        0x13900373
139002E8  488b4728                       mov       rax, qword ptr [rdi + 0x28]
139002EC  4885c0                         test      rax, rax
139002EF  0f8488000000                   je        0x1390037d
139002F5  f74018fcffffff                 test      dword ptr [rax + 0x18], 0xfffffffc
139002FC  0f8480000000                   je        0x13900382
13900302  c6402300                       mov       byte ptr [rax + 0x23], 0
13900306  488b07                         mov       rax, qword ptr [rdi]
13900309  0fb788c2000000                 movzx     ecx, word ptr [rax + 0xc2]
13900310  4c8b84c810010000               mov       r8, qword ptr [rax + rcx*8 + 0x110]
13900318  4889f9                         mov       rcx, rdi
1390031B  ba03000000                     mov       edx, 3
13900320  ff9010010000                   call      qword ptr [rax + 0x110]
13900326  4889f9                         mov       rcx, rdi
13900329  31d2                           xor       edx, edx
1390032B  e8609b6dfb                     call      0xefd9e90
13900330  4889f9                         mov       rcx, rdi
13900333  31d2                           xor       edx, edx
13900335  e8d6a76dfb                     call      0xefdab10
1390033A  0f57c9                         xorps     xmm1, xmm1
1390033D  4889f1                         mov       rcx, rsi
13900340  4883c430                       add       rsp, 0x30
13900344  5b                             pop       rbx
13900345  5f                             pop       rdi
13900346  5e                             pop       rsi
13900347  e944b6ffff                     jmp       0x138fb990
1390034C  90                             nop       
1390034D  4883c430                       add       rsp, 0x30
13900351  5b                             pop       rbx
13900352  5f                             pop       rdi
13900353  5e                             pop       rsi
13900354  c3                             ret       
13900355  e8c68d1ced                     call      0xac9120
1390035A  e8c18d1ced                     call      0xac9120
1390035F  e8bc1397ec                     call      0x271720
13900364  4889c1                         mov       rcx, rax
13900367  31d2                           xor       edx, edx
13900369  e8428d1ced                     call      0xac90b0
1390036E  e8ad8d1ced                     call      0xac9120
13900373  e8a88d1ced                     call      0xac9120
13900378  e8a38d1ced                     call      0xac9120
1390037D  e89e8d1ced                     call      0xac9120
13900382  e8991397ec                     call      0x271720
13900387  4889c1                         mov       rcx, rax
1390038A  31d2                           xor       edx, edx
1390038C  e81f8d1ced                     call      0xac90b0
13900391  e88a8d1ced                     call      0xac9120
13900396  e8858d1ced                     call      0xac9120
1390039B  cc                             int3      
