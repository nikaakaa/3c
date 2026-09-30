1434A4B0  56                             push      rsi
1434A4B1  57                             push      rdi
1434A4B2  53                             push      rbx
1434A4B3  4883ec40                       sub       rsp, 0x40
1434A4B7  0f29742430                     movaps    xmmword ptr [rsp + 0x30], xmm6
1434A4BC  4889ce                         mov       rsi, rcx
1434A4BF  803dfabe52f100                 cmp       byte ptr [rip - 0xead4106], 0 ; RIP_RVA=0x58763c0
1434A4C6  0f8427020000                   je        0x1434a6f3
1434A4CC  803d485807f100                 cmp       byte ptr [rip - 0xef8a7b8], 0 ; RIP_RVA=0x53bfd1b
1434A4D3  0f8538020000                   jne       0x1434a711
1434A4D9  807e6a00                       cmp       byte ptr [rsi + 0x6a], 0
1434A4DD  7406                           je        0x1434a4e5
1434A4DF  807e6c00                       cmp       byte ptr [rsi + 0x6c], 0
1434A4E3  740d                           je        0x1434a4f2
1434A4E5  0f28742430                     movaps    xmm6, xmmword ptr [rsp + 0x30]
1434A4EA  4883c440                       add       rsp, 0x40
1434A4EE  5b                             pop       rbx
1434A4EF  5f                             pop       rdi
1434A4F0  5e                             pop       rsi
1434A4F1  c3                             ret       
1434A4F2  c6466c01                       mov       byte ptr [rsi + 0x6c], 1
1434A4F6  488b059b8526f1                 mov       rax, qword ptr [rip - 0xed97a65] ; RIP_RVA=0x55b2a98
1434A4FD  488b38                         mov       rdi, qword ptr [rax]
1434A500  f687cc00000001                 test      byte ptr [rdi + 0xcc], 1
1434A507  0f842e020000                   je        0x1434a73b
1434A50D  488b4768                       mov       rax, qword ptr [rdi + 0x68]
1434A511  488b5810                       mov       rbx, qword ptr [rax + 0x10]
1434A515  4885db                         test      rbx, rbx
1434A518  0f8436020000                   je        0x1434a754
1434A51E  f683cc00000001                 test      byte ptr [rbx + 0xcc], 1
1434A525  0f8443020000                   je        0x1434a76e
1434A52B  488b4360                       mov       rax, qword ptr [rbx + 0x60]
1434A52F  488b38                         mov       rdi, qword ptr [rax]
1434A532  488b0d8f7811f1                 mov       rcx, qword ptr [rip - 0xeee8771] ; RIP_RVA=0x5461dc8
1434A539  80b9cb00000000                 cmp       byte ptr [rcx + 0xcb], 0
1434A540  0f8435020000                   je        0x1434a77b
1434A546  803dfbd552f100                 cmp       byte ptr [rip - 0xead2a05], 0 ; RIP_RVA=0x5877b48
1434A54D  0f843a020000                   je        0x1434a78d
1434A553  488b0d6e7811f1                 mov       rcx, qword ptr [rip - 0xeee8792] ; RIP_RVA=0x5461dc8
1434A55A  80b9cb00000000                 cmp       byte ptr [rcx + 0xcb], 0
1434A561  0f844b020000                   je        0x1434a7b2
1434A567  4885ff                         test      rdi, rdi
1434A56A  0f8450020000                   je        0x1434a7c0
1434A570  f30f1035982b02f1               movss     xmm6, dword ptr [rip - 0xefdd468] ; RIP_RVA=0x536d110
1434A578  803dea3e08f100                 cmp       byte ptr [rip - 0xef7c116], 0 ; RIP_RVA=0x53ce469
1434A57F  0f8540020000                   jne       0x1434a7c5
1434A585  488b4f18                       mov       rcx, qword ptr [rdi + 0x18]
1434A589  4885c9                         test      rcx, rcx
1434A58C  0f84c1020000                   je        0x1434a853
1434A592  48c744242000000000             mov       qword ptr [rsp + 0x20], 0
1434A59B  ba04000000                     mov       edx, 4
1434A5A0  0f28d6                         movaps    xmm2, xmm6
1434A5A3  4531c9                         xor       r9d, r9d
1434A5A6  e885f9c8fa                     call      0xefd9f30
1434A5AB  488b05e68426f1                 mov       rax, qword ptr [rip - 0xed97b1a] ; RIP_RVA=0x55b2a98
1434A5B2  488b38                         mov       rdi, qword ptr [rax]
1434A5B5  f687cc00000001                 test      byte ptr [rdi + 0xcc], 1
1434A5BC  0f8441020000                   je        0x1434a803
1434A5C2  488b4768                       mov       rax, qword ptr [rdi + 0x68]
1434A5C6  488b5810                       mov       rbx, qword ptr [rax + 0x10]
1434A5CA  4885db                         test      rbx, rbx
1434A5CD  0f8449020000                   je        0x1434a81c
1434A5D3  f683cc00000001                 test      byte ptr [rbx + 0xcc], 1
1434A5DA  0f8456020000                   je        0x1434a836
1434A5E0  488b4360                       mov       rax, qword ptr [rbx + 0x60]
1434A5E4  488b38                         mov       rdi, qword ptr [rax]
1434A5E7  4885ff                         test      rdi, rdi
1434A5EA  0f845e020000                   je        0x1434a84e
1434A5F0  803d8d3e08f100                 cmp       byte ptr [rip - 0xef7c173], 0 ; RIP_RVA=0x53ce484
1434A5F7  0f855b020000                   jne       0x1434a858
1434A5FD  0f57c9                         xorps     xmm1, xmm1
1434A600  4889f9                         mov       rcx, rdi
1434A603  e888135bff                     call      0x138fb990
1434A608  488b05898426f1                 mov       rax, qword ptr [rip - 0xed97b77] ; RIP_RVA=0x55b2a98
1434A60F  488b38                         mov       rdi, qword ptr [rax]
1434A612  f687cc00000001                 test      byte ptr [rdi + 0xcc], 1
1434A619  0f846e020000                   je        0x1434a88d
1434A61F  488b4768                       mov       rax, qword ptr [rdi + 0x68]
1434A623  488b5810                       mov       rbx, qword ptr [rax + 0x10]
1434A627  4885db                         test      rbx, rbx
1434A62A  0f8476020000                   je        0x1434a8a6
1434A630  f683cc00000001                 test      byte ptr [rbx + 0xcc], 1
1434A637  0f8483020000                   je        0x1434a8c0
1434A63D  488b4360                       mov       rax, qword ptr [rbx + 0x60]
1434A641  488b08                         mov       rcx, qword ptr [rax]
1434A644  4885c9                         test      rcx, rcx
1434A647  0f848b020000                   je        0x1434a8d8
1434A64D  e84e435bff                     call      0x138fe9a0
1434A652  4889f1                         mov       rcx, rsi
1434A655  e8f6020000                     call      0x1434a950 ; MoleMole.BattlePhotoSubsystem.SyncAllAnimatorSpeed()
1434A65A  488b0d577410f1                 mov       rcx, qword ptr [rip - 0xeef8ba9] ; RIP_RVA=0x5451ab8
1434A661  80b9cb00000000                 cmp       byte ptr [rcx + 0xcb], 0
1434A668  0f846f020000                   je        0x1434a8dd
1434A66E  803d78ad08f100                 cmp       byte ptr [rip - 0xef75288], 0 ; RIP_RVA=0x53d53ed
1434A675  0f8574020000                   jne       0x1434a8ef
1434A67B  488b05ae6001f1                 mov       rax, qword ptr [rip - 0xefe9f52] ; RIP_RVA=0x5360730
1434A682  488b80c8d50200                 mov       rax, qword ptr [rax + 0x2d5c8]
1434A689  4885c0                         test      rax, rax
1434A68C  0f8453feffff                   je        0x1434a4e5
1434A692  488b8000010000                 mov       rax, qword ptr [rax + 0x100]
1434A699  4885c0                         test      rax, rax
1434A69C  0f8443feffff                   je        0x1434a4e5
1434A6A2  488b7040                       mov       rsi, qword ptr [rax + 0x40]
1434A6A6  4885f6                         test      rsi, rsi
1434A6A9  0f8436feffff                   je        0x1434a4e5
1434A6AF  488b05caec10f1                 mov       rax, qword ptr [rip - 0xeef1336] ; RIP_RVA=0x5459380
1434A6B6  488b0e                         mov       rcx, qword ptr [rsi]
1434A6B9  0fb690c9000000                 movzx     edx, byte ptr [rax + 0xc9]
1434A6C0  3891c9000000                   cmp       byte ptr [rcx + 0xc9], dl
1434A6C6  0f8219feffff                   jb        0x1434a4e5
1434A6CC  488b4928                       mov       rcx, qword ptr [rcx + 0x28]
1434A6D0  483944d1f8                     cmp       qword ptr [rcx + rdx*8 - 8], rax
1434A6D5  0f850afeffff                   jne       0x1434a4e5
1434A6DB  803db86007f100                 cmp       byte ptr [rip - 0xef89f48], 0 ; RIP_RVA=0x53c079a
1434A6E2  0f852f020000                   jne       0x1434a917
1434A6E8  ff86f4020000                   inc       dword ptr [rsi + 0x2f4]
1434A6EE  e9f2fdffff                     jmp       0x1434a4e5
1434A6F3  b9d0490200                     mov       ecx, 0x249d0
1434A6F8  e8e3ecf2eb                     call      0x2793e0
1434A6FD  c605bcbc52f101                 mov       byte ptr [rip - 0xead4344], 1 ; RIP_RVA=0x58763c0
1434A704  803d105607f100                 cmp       byte ptr [rip - 0xef8a9f0], 0 ; RIP_RVA=0x53bfd1b
1434A70B  0f84c8fdffff                   je        0x1434a4d9
1434A711  b96b2a0200                     mov       ecx, 0x22a6b
1434A716  e84530e3fb                     call      0x1017d760
1434A71B  4885c0                         test      rax, rax
1434A71E  0f840b020000                   je        0x1434a92f
1434A724  4889c1                         mov       rcx, rax
1434A727  4889f2                         mov       rdx, rsi
1434A72A  0f28742430                     movaps    xmm6, xmmword ptr [rsp + 0x30]
1434A72F  4883c440                       add       rsp, 0x40
1434A733  5b                             pop       rbx
1434A734  5f                             pop       rdi
1434A735  5e                             pop       rsi
1434A736  e935f1e3f6                     jmp       0xb189870
1434A73B  4889f9                         mov       rcx, rdi
1434A73E  e85d46f2eb                     call      0x26eda0
1434A743  488b4768                       mov       rax, qword ptr [rdi + 0x68]
1434A747  488b5810                       mov       rbx, qword ptr [rax + 0x10]
1434A74B  4885db                         test      rbx, rbx
1434A74E  0f85cafdffff                   jne       0x1434a51e
1434A754  4889f9                         mov       rcx, rdi
1434A757  31d2                           xor       edx, edx
1434A759  e8f257f2eb                     call      0x26ff50
1434A75E  488b18                         mov       rbx, qword ptr [rax]
1434A761  f683cc00000001                 test      byte ptr [rbx + 0xcc], 1
1434A768  0f85bdfdffff                   jne       0x1434a52b
1434A76E  4889d9                         mov       rcx, rbx
1434A771  e82a46f2eb                     call      0x26eda0
1434A776  e9b0fdffff                     jmp       0x1434a52b
1434A77B  e8d076f2eb                     call      0x271e50
1434A780  803dc1d352f100                 cmp       byte ptr [rip - 0xead2c3f], 0 ; RIP_RVA=0x5877b48
1434A787  0f85c6fdffff                   jne       0x1434a553
1434A78D  b958610200                     mov       ecx, 0x26158
1434A792  e849ecf2eb                     call      0x2793e0
1434A797  c605aad352f101                 mov       byte ptr [rip - 0xead2c56], 1 ; RIP_RVA=0x5877b48
1434A79E  488b0d237611f1                 mov       rcx, qword ptr [rip - 0xeee89dd] ; RIP_RVA=0x5461dc8
1434A7A5  80b9cb00000000                 cmp       byte ptr [rcx + 0xcb], 0
1434A7AC  0f85b5fdffff                   jne       0x1434a567
1434A7B2  e89976f2eb                     call      0x271e50
1434A7B7  4885ff                         test      rdi, rdi
1434A7BA  0f85b0fdffff                   jne       0x1434a570
1434A7C0  e85be977ec                     call      0xac9120
1434A7C5  b9b9110300                     mov       ecx, 0x311b9
1434A7CA  e8912fe3fb                     call      0x1017d760
1434A7CF  4885c0                         test      rax, rax
1434A7D2  0f845c010000                   je        0x1434a934
1434A7D8  4889c1                         mov       rcx, rax
1434A7DB  4889fa                         mov       rdx, rdi
1434A7DE  0f28d6                         movaps    xmm2, xmm6
1434A7E1  41b904000000                   mov       r9d, 4
1434A7E7  e8d4f3ecf6                     call      0xb219bc0
1434A7EC  488b05a58226f1                 mov       rax, qword ptr [rip - 0xed97d5b] ; RIP_RVA=0x55b2a98
1434A7F3  488b38                         mov       rdi, qword ptr [rax]
1434A7F6  f687cc00000001                 test      byte ptr [rdi + 0xcc], 1
1434A7FD  0f85bffdffff                   jne       0x1434a5c2
1434A803  4889f9                         mov       rcx, rdi
1434A806  e89545f2eb                     call      0x26eda0
1434A80B  488b4768                       mov       rax, qword ptr [rdi + 0x68]
1434A80F  488b5810                       mov       rbx, qword ptr [rax + 0x10]
1434A813  4885db                         test      rbx, rbx
1434A816  0f85b7fdffff                   jne       0x1434a5d3
1434A81C  4889f9                         mov       rcx, rdi
1434A81F  31d2                           xor       edx, edx
1434A821  e82a57f2eb                     call      0x26ff50
1434A826  488b18                         mov       rbx, qword ptr [rax]
1434A829  f683cc00000001                 test      byte ptr [rbx + 0xcc], 1
1434A830  0f85aafdffff                   jne       0x1434a5e0
1434A836  4889d9                         mov       rcx, rbx
1434A839  e86245f2eb                     call      0x26eda0
1434A83E  488b4360                       mov       rax, qword ptr [rbx + 0x60]
1434A842  488b38                         mov       rdi, qword ptr [rax]
1434A845  4885ff                         test      rdi, rdi
1434A848  0f85a2fdffff                   jne       0x1434a5f0
1434A84E  e8cde877ec                     call      0xac9120
1434A853  e8c8e877ec                     call      0xac9120
1434A858  b9d4110300                     mov       ecx, 0x311d4
1434A85D  e8fe2ee3fb                     call      0x1017d760
1434A862  4885c0                         test      rax, rax
1434A865  0f84ce000000                   je        0x1434a939
1434A86B  4889c1                         mov       rcx, rax
1434A86E  4889fa                         mov       rdx, rdi
1434A871  e8faefe3f6                     call      0xb189870
1434A876  488b051b8226f1                 mov       rax, qword ptr [rip - 0xed97de5] ; RIP_RVA=0x55b2a98
1434A87D  488b38                         mov       rdi, qword ptr [rax]
1434A880  f687cc00000001                 test      byte ptr [rdi + 0xcc], 1
1434A887  0f8592fdffff                   jne       0x1434a61f
1434A88D  4889f9                         mov       rcx, rdi
1434A890  e80b45f2eb                     call      0x26eda0
1434A895  488b4768                       mov       rax, qword ptr [rdi + 0x68]
1434A899  488b5810                       mov       rbx, qword ptr [rax + 0x10]
1434A89D  4885db                         test      rbx, rbx
1434A8A0  0f858afdffff                   jne       0x1434a630
1434A8A6  4889f9                         mov       rcx, rdi
1434A8A9  31d2                           xor       edx, edx
1434A8AB  e8a056f2eb                     call      0x26ff50
1434A8B0  488b18                         mov       rbx, qword ptr [rax]
1434A8B3  f683cc00000001                 test      byte ptr [rbx + 0xcc], 1
1434A8BA  0f857dfdffff                   jne       0x1434a63d
1434A8C0  4889d9                         mov       rcx, rbx
1434A8C3  e8d844f2eb                     call      0x26eda0
1434A8C8  488b4360                       mov       rax, qword ptr [rbx + 0x60]
1434A8CC  488b08                         mov       rcx, qword ptr [rax]
1434A8CF  4885c9                         test      rcx, rcx
1434A8D2  0f8575fdffff                   jne       0x1434a64d
1434A8D8  e843e877ec                     call      0xac9120
1434A8DD  e86e75f2eb                     call      0x271e50
1434A8E2  803d04ab08f100                 cmp       byte ptr [rip - 0xef754fc], 0 ; RIP_RVA=0x53d53ed
1434A8E9  0f848cfdffff                   je        0x1434a67b
1434A8EF  b93d810300                     mov       ecx, 0x3813d
1434A8F4  e8672ee3fb                     call      0x1017d760
1434A8F9  4885c0                         test      rax, rax
1434A8FC  7440                           je        0x1434a93e
1434A8FE  4889c1                         mov       rcx, rax
1434A901  e81a08e4f6                     call      0xb18b120
1434A906  4889c6                         mov       rsi, rax
1434A909  4885f6                         test      rsi, rsi
1434A90C  0f859dfdffff                   jne       0x1434a6af
1434A912  e9cefbffff                     jmp       0x1434a4e5
1434A917  b9ea340200                     mov       ecx, 0x234ea
1434A91C  e83f2ee3fb                     call      0x1017d760
1434A921  4885c0                         test      rax, rax
1434A924  0f85fafdffff                   jne       0x1434a724
1434A92A  e8f1e777ec                     call      0xac9120
1434A92F  e8ece777ec                     call      0xac9120
1434A934  e8e7e777ec                     call      0xac9120
1434A939  e8e2e777ec                     call      0xac9120
1434A93E  e8dde777ec                     call      0xac9120
1434A943  cc                             int3      
