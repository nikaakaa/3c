138FE170  56                             push      rsi
138FE171  57                             push      rdi
138FE172  4883ec48                       sub       rsp, 0x48
138FE176  0f297c2430                     movaps    xmmword ptr [rsp + 0x30], xmm7
138FE17B  0f29742420                     movaps    xmmword ptr [rsp + 0x20], xmm6
138FE180  0f28f2                         movaps    xmm6, xmm2
138FE183  4889ce                         mov       rsi, rcx
138FE186  803d9902adf100                 cmp       byte ptr [rip - 0xe52fd67], 0 ; RIP_RVA=0x53ce426
138FE18D  0f859f000000                   jne       0x138fe232
138FE193  488b7e50                       mov       rdi, qword ptr [rsi + 0x50]
138FE197  4885ff                         test      rdi, rdi
138FE19A  0f84c5000000                   je        0x138fe265
138FE1A0  807f3900                       cmp       byte ptr [rdi + 0x39], 0
138FE1A4  7442                           je        0x138fe1e8
138FE1A6  807f3100                       cmp       byte ptr [rdi + 0x31], 0
138FE1AA  744d                           je        0x138fe1f9
138FE1AC  803d7702adf100                 cmp       byte ptr [rip - 0xe52fd89], 0 ; RIP_RVA=0x53ce42a
138FE1B3  0f85b1000000                   jne       0x138fe26a
138FE1B9  4889f1                         mov       rcx, rsi
138FE1BC  e87f030000                     call      0x138fe540
138FE1C1  488b4e50                       mov       rcx, qword ptr [rsi + 0x50]
138FE1C5  4885c9                         test      rcx, rcx
138FE1C8  0f84c3000000                   je        0x138fe291
138FE1CE  0f57d2                         xorps     xmm2, xmm2
138FE1D1  31d2                           xor       edx, edx
138FE1D3  0f28742420                     movaps    xmm6, xmmword ptr [rsp + 0x20]
138FE1D8  0f287c2430                     movaps    xmm7, xmmword ptr [rsp + 0x30]
138FE1DD  4883c448                       add       rsp, 0x48
138FE1E1  5f                             pop       rdi
138FE1E2  5e                             pop       rsi
138FE1E3  e9486f0201                     jmp       0x14925130 ; BCFKAGODFJI.HHDCJCHLPAK(bool CLGALOBICEO, float AOKLEKIILII)
138FE1E8  0f28742420                     movaps    xmm6, xmmword ptr [rsp + 0x20]
138FE1ED  0f287c2430                     movaps    xmm7, xmmword ptr [rsp + 0x30]
138FE1F2  4883c448                       add       rsp, 0x48
138FE1F6  5f                             pop       rdi
138FE1F7  5e                             pop       rsi
138FE1F8  c3                             ret       
138FE1F9  f30f59b6e8000000               mulss     xmm6, dword ptr [rsi + 0xe8]
138FE201  803dcd11aef100                 cmp       byte ptr [rip - 0xe51ee33], 0 ; RIP_RVA=0x53df3d5
138FE208  0f8588000000                   jne       0x138fe296
138FE20E  ff1584cab0f1                   call      qword ptr [rip - 0xe4f357c] ; RIP_RVA=0x540ac98
138FE214  4889f9                         mov       rcx, rdi
138FE217  0f28ce                         movaps    xmm1, xmm6
138FE21A  0f28d0                         movaps    xmm2, xmm0
138FE21D  0f28742420                     movaps    xmm6, xmmword ptr [rsp + 0x20]
138FE222  0f287c2430                     movaps    xmm7, xmmword ptr [rsp + 0x30]
138FE227  4883c448                       add       rsp, 0x48
138FE22B  5f                             pop       rdi
138FE22C  5e                             pop       rsi
138FE22D  e9ce6b0201                     jmp       0x14924e00 ; BCFKAGODFJI.NDJEFNGILLL(float BADFOMGICBI, float NGJOJICJBJM)
138FE232  0f28f9                         movaps    xmm7, xmm1
138FE235  b976110300                     mov       ecx, 0x31176
138FE23A  e821f587fc                     call      0x1017d760
138FE23F  4885c0                         test      rax, rax
138FE242  747f                           je        0x138fe2c3
138FE244  4889c1                         mov       rcx, rax
138FE247  4889f2                         mov       rdx, rsi
138FE24A  0f28d7                         movaps    xmm2, xmm7
138FE24D  0f28de                         movaps    xmm3, xmm6
138FE250  0f28742420                     movaps    xmm6, xmmword ptr [rsp + 0x20]
138FE255  0f287c2430                     movaps    xmm7, xmmword ptr [rsp + 0x30]
138FE25A  4883c448                       add       rsp, 0x48
138FE25E  5f                             pop       rdi
138FE25F  5e                             pop       rsi
138FE260  e98b488df7                     jmp       0xb1d2af0
138FE265  e8b6ae1ced                     call      0xac9120
138FE26A  b97a110300                     mov       ecx, 0x3117a
138FE26F  e8ecf487fc                     call      0x1017d760
138FE274  4885c0                         test      rax, rax
138FE277  744f                           je        0x138fe2c8
138FE279  4889c1                         mov       rcx, rax
138FE27C  4889f2                         mov       rdx, rsi
138FE27F  e8ecb588f7                     call      0xb189870
138FE284  488b4e50                       mov       rcx, qword ptr [rsi + 0x50]
138FE288  4885c9                         test      rcx, rcx
138FE28B  0f853dffffff                   jne       0x138fe1ce
138FE291  e88aae1ced                     call      0xac9120
138FE296  b925210400                     mov       ecx, 0x42125
138FE29B  e8c0f487fc                     call      0x1017d760
138FE2A0  4885c0                         test      rax, rax
138FE2A3  7428                           je        0x138fe2cd
138FE2A5  4889c1                         mov       rcx, rax
138FE2A8  4889fa                         mov       rdx, rdi
138FE2AB  0f28d6                         movaps    xmm2, xmm6
138FE2AE  0f28742420                     movaps    xmm6, xmmword ptr [rsp + 0x20]
138FE2B3  0f287c2430                     movaps    xmm7, xmmword ptr [rsp + 0x30]
138FE2B8  4883c448                       add       rsp, 0x48
138FE2BC  5f                             pop       rdi
138FE2BD  5e                             pop       rsi
138FE2BE  e97dc288f7                     jmp       0xb18a540
138FE2C3  e858ae1ced                     call      0xac9120
138FE2C8  e853ae1ced                     call      0xac9120
138FE2CD  e84eae1ced                     call      0xac9120
138FE2D2  cc                             int3      
