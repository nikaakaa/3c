174DE1B0  56                             push      rsi
174DE1B1  57                             push      rdi
174DE1B2  4883ec28                       sub       rsp, 0x28
174DE1B6  4889d6                         mov       rsi, rdx
174DE1B9  4889cf                         mov       rdi, rcx
174DE1BC  803d61a5eeed00                 cmp       byte ptr [rip - 0x12115a9f], 0 ; RIP_RVA=0x53c8724
174DE1C3  7522                           jne       0x174de1e7
174DE1C5  4889f9                         mov       rcx, rdi
174DE1C8  4889f2                         mov       rdx, rsi
174DE1CB  e800060000                     call      0x174de7d0
174DE1D0  84c0                           test      al, al
174DE1D2  740c                           je        0x174de1e0
174DE1D4  4885f6                         test      rsi, rsi
174DE1D7  7431                           je        0x174de20a
174DE1D9  c686e400000001                 mov       byte ptr [rsi + 0xe4], 1
174DE1E0  4883c428                       add       rsp, 0x28
174DE1E4  5f                             pop       rdi
174DE1E5  5e                             pop       rsi
174DE1E6  c3                             ret       
174DE1E7  b974b40200                     mov       ecx, 0x2b474
174DE1EC  e86ff5c9f8                     call      0x1017d760
174DE1F1  4885c0                         test      rax, rax
174DE1F4  7419                           je        0x174de20f
174DE1F6  4889c1                         mov       rcx, rax
174DE1F9  4889fa                         mov       rdx, rdi
174DE1FC  4989f0                         mov       r8, rsi
174DE1FF  4883c428                       add       rsp, 0x28
174DE203  5f                             pop       rdi
174DE204  5e                             pop       rsi
174DE205  e9d6becaf3                     jmp       0xb18a0e0
174DE20A  e811af5ee9                     call      0xac9120
174DE20F  e80caf5ee9                     call      0xac9120
174DE214  cc                             int3      
