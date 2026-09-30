174DE220  4157                           push      r15
174DE222  4156                           push      r14
174DE224  56                             push      rsi
174DE225  57                             push      rdi
174DE226  53                             push      rbx
174DE227  4881eca0000000                 sub       rsp, 0xa0
174DE22E  440f29ac2490000000             movaps    xmmword ptr [rsp + 0x90], xmm13
174DE237  440f29a42480000000             movaps    xmmword ptr [rsp + 0x80], xmm12
174DE240  440f295c2470                   movaps    xmmword ptr [rsp + 0x70], xmm11
174DE246  440f29542460                   movaps    xmmword ptr [rsp + 0x60], xmm10
174DE24C  440f294c2450                   movaps    xmmword ptr [rsp + 0x50], xmm9
174DE252  440f29442440                   movaps    xmmword ptr [rsp + 0x40], xmm8
174DE258  0f297c2430                     movaps    xmmword ptr [rsp + 0x30], xmm7
174DE25D  0f29742420                     movaps    xmmword ptr [rsp + 0x20], xmm6
174DE262  0f28f1                         movaps    xmm6, xmm1
174DE265  4889ce                         mov       rsi, rcx
174DE268  803d63703aee00                 cmp       byte ptr [rip - 0x11c58f9d], 0 ; RIP_RVA=0x58852d2
174DE26F  0f8498030000                   je        0x174de60d
174DE275  803daea4eeed00                 cmp       byte ptr [rip - 0x12115b52], 0 ; RIP_RVA=0x53c872a
174DE27C  0f85a9030000                   jne       0x174de62b
174DE282  4c8b4620                       mov       r8, qword ptr [rsi + 0x20]
174DE286  4d85c0                         test      r8, r8
174DE289  0f844c030000                   je        0x174de5db
174DE28F  f30f103d6d4ee8ed               movss     xmm7, dword ptr [rip - 0x1217b193] ; RIP_RVA=0x5363104
174DE297  f2440f100d5c4ee8ed             movsd     xmm9, qword ptr [rip - 0x1217b1a4] ; RIP_RVA=0x53630fc
174DE2A0  0f28c7                         movaps    xmm0, xmm7
174DE2A3  410fc6c130                     shufps    xmm0, xmm9, 0x30
174DE2A8  450f28c1                       movaps    xmm8, xmm9
174DE2AC  440fc6c081                     shufps    xmm8, xmm0, 0x81
174DE2B1  450fc6c024                     shufps    xmm8, xmm8, 0x24
174DE2B6  4531f6                         xor       r14d, r14d
174DE2B9  440f28d7                       movaps    xmm10, xmm7
174DE2BD  440f28e7                       movaps    xmm12, xmm7
174DE2C1  31db                           xor       ebx, ebx
174DE2C3  4531ff                         xor       r15d, r15d
174DE2C6  450f28e9                       movaps    xmm13, xmm9
174DE2CA  450f28d9                       movaps    xmm11, xmm9
174DE2CE  eb3e                           jmp       0x174de30e
174DE2D0  f20f104778                     movsd     xmm0, qword ptr [rdi + 0x78]
174DE2D5  f20f108ff4000000               movsd     xmm1, qword ptr [rdi + 0xf4]
174DE2DD  0fc6c111                       shufps    xmm0, xmm1, 0x11
174DE2E1  f3440f588ff0000000             addss     xmm9, dword ptr [rdi + 0xf0]
174DE2EA  f30f58bf80000000               addss     xmm7, dword ptr [rdi + 0x80]
174DE2F2  440f58c0                       addps     xmm8, xmm0
174DE2F6  4c89ff                         mov       rdi, r15
174DE2F9  ffc3                           inc       ebx
174DE2FB  4c8b4620                       mov       r8, qword ptr [rsi + 0x20]
174DE2FF  4989ff                         mov       r15, rdi
174DE302  49ffc6                         inc       r14
174DE305  4d85c0                         test      r8, r8
174DE308  0f84cd020000                   je        0x174de5db
174DE30E  498b8008040000                 mov       rax, qword ptr [r8 + 0x408]
174DE315  4885c0                         test      rax, rax
174DE318  0f84c2020000                   je        0x174de5e0
174DE31E  488b4820                       mov       rcx, qword ptr [rax + 0x20]
174DE322  4885c9                         test      rcx, rcx
174DE325  0f84ba020000                   je        0x174de5e5
174DE32B  48635118                       movsxd    rdx, dword ptr [rcx + 0x18]
174DE32F  4939d6                         cmp       r14, rdx
174DE332  0f8d2a010000                   jge       0x174de462
174DE338  89d0                           mov       eax, edx
174DE33A  4939c6                         cmp       r14, rax
174DE33D  0f8351030000                   jae       0x174de694
174DE343  488b4110                       mov       rax, qword ptr [rcx + 0x10]
174DE347  4a8b7cf020                     mov       rdi, qword ptr [rax + r14*8 + 0x20]
174DE34C  4885ff                         test      rdi, rdi
174DE34F  0f8495020000                   je        0x174de5ea
174DE355  80bfe500000000                 cmp       byte ptr [rdi + 0xe5], 0
174DE35C  74a4                           je        0x174de302
174DE35E  4889f9                         mov       rcx, rdi
174DE361  0f28ce                         movaps    xmm1, xmm6
174DE364  e897095afb                     call      0x12a7ed00
174DE369  8b87d8000000                   mov       eax, dword ptr [rdi + 0xd8]
174DE36F  83f801                         cmp       eax, 1
174DE372  0f8458ffffff                   je        0x174de2d0
174DE378  85c0                           test      eax, eax
174DE37A  0f8576ffffff                   jne       0x174de2f6
174DE380  488b4620                       mov       rax, qword ptr [rsi + 0x20]
174DE384  4885c0                         test      rax, rax
174DE387  0f8476020000                   je        0x174de603
174DE38D  488b8008040000                 mov       rax, qword ptr [rax + 0x408]
174DE394  4885c0                         test      rax, rax
174DE397  0f846b020000                   je        0x174de608
174DE39D  80783600                       cmp       byte ptr [rax + 0x36], 0
174DE3A1  7430                           je        0x174de3d3
174DE3A3  807f6900                       cmp       byte ptr [rdi + 0x69], 0
174DE3A7  0f8449ffffff                   je        0x174de2f6
174DE3AD  f2440f10aff0000000             movsd     xmm13, qword ptr [rdi + 0xf0]
174DE3B6  f3440f10a7f8000000             movss     xmm12, dword ptr [rdi + 0xf8]
174DE3BF  f2440f105f78                   movsd     xmm11, qword ptr [rdi + 0x78]
174DE3C5  f3440f109780000000             movss     xmm10, dword ptr [rdi + 0x80]
174DE3CE  e926ffffff                     jmp       0x174de2f9
174DE3D3  c6476900                       mov       byte ptr [rdi + 0x69], 0
174DE3D7  4d85ff                         test      r15, r15
174DE3DA  740f                           je        0x174de3eb
174DE3DC  418b8790000000                 mov       eax, dword ptr [r15 + 0x90]
174DE3E3  398790000000                   cmp       dword ptr [rdi + 0x90], eax
174DE3E9  7545                           jne       0x174de430
174DE3EB  f20f1087f0000000               movsd     xmm0, qword ptr [rdi + 0xf0]
174DE3F3  f30f108ff8000000               movss     xmm1, dword ptr [rdi + 0xf8]
174DE3FB  0f28d0                         movaps    xmm2, xmm0
174DE3FE  0f59d0                         mulps     xmm2, xmm0
174DE401  410f28dd                       movaps    xmm3, xmm13
174DE405  410f59dd                       mulps     xmm3, xmm13
174DE409  f20f7cd3                       haddps    xmm2, xmm3
174DE40D  0f28d9                         movaps    xmm3, xmm1
174DE410  410f14dc                       unpcklps  xmm3, xmm12
174DE414  0f59db                         mulps     xmm3, xmm3
174DE417  0fc6d2e8                       shufps    xmm2, xmm2, 0xe8
174DE41B  0f58d3                         addps     xmm2, xmm3
174DE41E  0f51d2                         sqrtps    xmm2, xmm2
174DE421  f30f16da                       movshdup  xmm3, xmm2
174DE425  0f2ed3                         ucomiss   xmm2, xmm3
174DE428  0f86c8feffff                   jbe       0x174de2f6
174DE42E  eb16                           jmp       0x174de446
174DE430  0f8ec0feffff                   jle       0x174de2f6
174DE436  f20f1087f0000000               movsd     xmm0, qword ptr [rdi + 0xf0]
174DE43E  f30f108ff8000000               movss     xmm1, dword ptr [rdi + 0xf8]
174DE446  f2440f105f78                   movsd     xmm11, qword ptr [rdi + 0x78]
174DE44C  f3440f109780000000             movss     xmm10, dword ptr [rdi + 0x80]
174DE455  440f28e1                       movaps    xmm12, xmm1
174DE459  440f28e8                       movaps    xmm13, xmm0
174DE45D  e997feffff                     jmp       0x174de2f9
174DE462  4d85ff                         test      r15, r15
174DE465  7415                           je        0x174de47c
174DE467  41c6476901                     mov       byte ptr [r15 + 0x69], 1
174DE46C  80783600                       cmp       byte ptr [rax + 0x36], 0
174DE470  740a                           je        0x174de47c
174DE472  80783500                       cmp       byte ptr [rax + 0x35], 0
174DE476  7504                           jne       0x174de47c
174DE478  c6403501                       mov       byte ptr [rax + 0x35], 1
174DE47C  31ff                           xor       edi, edi
174DE47E  eb1e                           jmp       0x174de49e
174DE480  8b92e0000000                   mov       edx, dword ptr [rdx + 0xe0]
174DE486  4531c0                         xor       r8d, r8d
174DE489  e8b25654f6                     call      0xda23b40
174DE48E  4c8b4620                       mov       r8, qword ptr [rsi + 0x20]
174DE492  48ffc7                         inc       rdi
174DE495  4d85c0                         test      r8, r8
174DE498  0f8460010000                   je        0x174de5fe
174DE49E  498b8008040000                 mov       rax, qword ptr [r8 + 0x408]
174DE4A5  4885c0                         test      rax, rax
174DE4A8  0f8441010000                   je        0x174de5ef
174DE4AE  488b4820                       mov       rcx, qword ptr [rax + 0x20]
174DE4B2  4885c9                         test      rcx, rcx
174DE4B5  0f8439010000                   je        0x174de5f4
174DE4BB  48635118                       movsxd    rdx, dword ptr [rcx + 0x18]
174DE4BF  4839d7                         cmp       rdi, rdx
174DE4C2  7d34                           jge       0x174de4f8
174DE4C4  89d2                           mov       edx, edx
174DE4C6  4839d7                         cmp       rdi, rdx
174DE4C9  0f83c5010000                   jae       0x174de694
174DE4CF  488b4910                       mov       rcx, qword ptr [rcx + 0x10]
174DE4D3  488b54f920                     mov       rdx, qword ptr [rcx + rdi*8 + 0x20]
174DE4D8  4885d2                         test      rdx, rdx
174DE4DB  0f8418010000                   je        0x174de5f9
174DE4E1  80bae500000000                 cmp       byte ptr [rdx + 0xe5], 0
174DE4E8  75a8                           jne       0x174de492
174DE4EA  488b4810                       mov       rcx, qword ptr [rax + 0x10]
174DE4EE  4885c9                         test      rcx, rcx
174DE4F1  758d                           jne       0x174de480
174DE4F3  e828ac5ee9                     call      0xac9120
174DE4F8  85db                           test      ebx, ebx
174DE4FA  7411                           je        0x174de50d
174DE4FC  8b4830                         mov       ecx, dword ptr [rax + 0x30]
174DE4FF  85c9                           test      ecx, ecx
174DE501  741c                           je        0x174de51f
174DE503  83f902                         cmp       ecx, 2
174DE506  7521                           jne       0x174de529
174DE508  0f57c0                         xorps     xmm0, xmm0
174DE50B  eb21                           jmp       0x174de52e
174DE50D  c6403400                       mov       byte ptr [rax + 0x34], 0
174DE511  c7403800000000                 mov       dword ptr [rax + 0x38], 0
174DE518  8b4830                         mov       ecx, dword ptr [rax + 0x30]
174DE51B  85c9                           test      ecx, ecx
174DE51D  75e4                           jne       0x174de503
174DE51F  f30f1005555a22eb               movss     xmm0, dword ptr [rip - 0x14dda5ab] ; RIP_RVA=0x2703f7c
174DE527  eb05                           jmp       0x174de52e
174DE529  f30f104050                     movss     xmm0, dword ptr [rax + 0x50]
174DE52E  410f28d0                       movaps    xmm2, xmm8
174DE532  66410f15d0                     unpckhpd  xmm2, xmm8
174DE537  f3440f58e2                     addss     xmm12, xmm2
174DE53C  f3440f59e0                     mulss     xmm12, xmm0
174DE541  f3410f10d1                     movss     xmm2, xmm9
174DE546  410f58d5                       addps     xmm2, xmm13
174DE54A  f30f12c8                       movsldup  xmm1, xmm0
174DE54E  0f59d1                         mulps     xmm2, xmm1
174DE551  0f135054                       movlps    qword ptr [rax + 0x54], xmm2
174DE555  f3440f11605c                   movss     dword ptr [rax + 0x5c], xmm12
174DE55B  488b4620                       mov       rax, qword ptr [rsi + 0x20]
174DE55F  4885c0                         test      rax, rax
174DE562  0f8422010000                   je        0x174de68a
174DE568  488b8008040000                 mov       rax, qword ptr [rax + 0x408]
174DE56F  4885c0                         test      rax, rax
174DE572  0f8417010000                   je        0x174de68f
174DE578  f3410f58fa                     addss     xmm7, xmm10
174DE57D  f30f59f8                       mulss     xmm7, xmm0
174DE581  450fc6c0e1                     shufps    xmm8, xmm8, 0xe1
174DE586  450f58c3                       addps     xmm8, xmm11
174DE58A  440f59c1                       mulps     xmm8, xmm1
174DE58E  440f134040                     movlps    qword ptr [rax + 0x40], xmm8
174DE593  f30f117848                     movss     dword ptr [rax + 0x48], xmm7
174DE598  0f28742420                     movaps    xmm6, xmmword ptr [rsp + 0x20]
174DE59D  0f287c2430                     movaps    xmm7, xmmword ptr [rsp + 0x30]
174DE5A2  440f28442440                   movaps    xmm8, xmmword ptr [rsp + 0x40]
174DE5A8  440f284c2450                   movaps    xmm9, xmmword ptr [rsp + 0x50]
174DE5AE  440f28542460                   movaps    xmm10, xmmword ptr [rsp + 0x60]
174DE5B4  440f285c2470                   movaps    xmm11, xmmword ptr [rsp + 0x70]
174DE5BA  440f28a42480000000             movaps    xmm12, xmmword ptr [rsp + 0x80]
174DE5C3  440f28ac2490000000             movaps    xmm13, xmmword ptr [rsp + 0x90]
174DE5CC  4881c4a0000000                 add       rsp, 0xa0
174DE5D3  5b                             pop       rbx
174DE5D4  5f                             pop       rdi
174DE5D5  5e                             pop       rsi
174DE5D6  415e                           pop       r14
174DE5D8  415f                           pop       r15
174DE5DA  c3                             ret       
174DE5DB  e840ab5ee9                     call      0xac9120
174DE5E0  e83bab5ee9                     call      0xac9120
174DE5E5  e836ab5ee9                     call      0xac9120
174DE5EA  e831ab5ee9                     call      0xac9120
174DE5EF  e82cab5ee9                     call      0xac9120
174DE5F4  e827ab5ee9                     call      0xac9120
174DE5F9  e822ab5ee9                     call      0xac9120
174DE5FE  e81dab5ee9                     call      0xac9120
174DE603  e818ab5ee9                     call      0xac9120
174DE608  e813ab5ee9                     call      0xac9120
174DE60D  b9e2380300                     mov       ecx, 0x338e2
174DE612  e8c9add9e8                     call      0x2793e0
174DE617  c605b46c3aee01                 mov       byte ptr [rip - 0x11c5934c], 1 ; RIP_RVA=0x58852d2
174DE61E  803d05a1eeed00                 cmp       byte ptr [rip - 0x12115efb], 0 ; RIP_RVA=0x53c872a
174DE625  0f8457fcffff                   je        0x174de282
174DE62B  b97ab40200                     mov       ecx, 0x2b47a
174DE630  e82bf1c9f8                     call      0x1017d760
174DE635  4885c0                         test      rax, rax
174DE638  745f                           je        0x174de699
174DE63A  4889c1                         mov       rcx, rax
174DE63D  4889f2                         mov       rdx, rsi
174DE640  0f28d6                         movaps    xmm2, xmm6
174DE643  0f28742420                     movaps    xmm6, xmmword ptr [rsp + 0x20]
174DE648  0f287c2430                     movaps    xmm7, xmmword ptr [rsp + 0x30]
174DE64D  440f28442440                   movaps    xmm8, xmmword ptr [rsp + 0x40]
174DE653  440f284c2450                   movaps    xmm9, xmmword ptr [rsp + 0x50]
174DE659  440f28542460                   movaps    xmm10, xmmword ptr [rsp + 0x60]
174DE65F  440f285c2470                   movaps    xmm11, xmmword ptr [rsp + 0x70]
174DE665  440f28a42480000000             movaps    xmm12, xmmword ptr [rsp + 0x80]
174DE66E  440f28ac2490000000             movaps    xmm13, xmmword ptr [rsp + 0x90]
174DE677  4881c4a0000000                 add       rsp, 0xa0
174DE67E  5b                             pop       rbx
174DE67F  5f                             pop       rdi
174DE680  5e                             pop       rsi
174DE681  415e                           pop       r14
174DE683  415f                           pop       r15
174DE685  e9b6becaf3                     jmp       0xb18a540
174DE68A  e891aa5ee9                     call      0xac9120
174DE68F  e88caa5ee9                     call      0xac9120
174DE694  e817b4f306                     call      0x1e419ab0
174DE699  e882aa5ee9                     call      0xac9120
174DE69E  cc                             int3      
