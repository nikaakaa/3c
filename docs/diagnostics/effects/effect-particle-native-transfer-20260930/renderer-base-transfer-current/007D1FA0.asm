007D1FA0  48895c2418                     mov       qword ptr [rsp + 0x18], rbx
007D1FA5  55                             push      rbp
007D1FA6  56                             push      rsi
007D1FA7  57                             push      rdi
007D1FA8  488bec                         mov       rbp, rsp
007D1FAB  4883ec30                       sub       rsp, 0x30
007D1FAF  488bda                         mov       rbx, rdx
007D1FB2  488bf9                         mov       rdi, rcx
007D1FB5  e86640ecff                     call      0x696020
007D1FBA  4c8b057f873601                 mov       r8, qword ptr [rip + 0x136877f] ; RIP_RVA=0x1b3a740
007D1FC1  4c8d4d20                       lea       r9, [rbp + 0x20]
007D1FC5  488d1514cd3001                 lea       rdx, [rip + 0x130cd14] ; RIP_RVA=0x1adece0
007D1FCC  c644242000                     mov       byte ptr [rsp + 0x20], 0
007D1FD1  488bcb                         mov       rcx, rbx
007D1FD4  e8f7044000                     call      0xbd24d0
007D1FD9  85c0                           test      eax, eax
007D1FDB  747b                           je        0x7d2058
007D1FDD  83f801                         cmp       eax, 1
007D1FE0  7c59                           jl        0x7d203b
007D1FE2  488b83d0000000                 mov       rax, qword ptr [rbx + 0xd0]
007D1FE9  4c8b4350                       mov       r8, qword ptr [rbx + 0x50]
007D1FED  48635368                       movsxd    rdx, dword ptr [rbx + 0x68]
007D1FF1  480faf5370                     imul      rdx, qword ptr [rbx + 0x70]
007D1FF6  488b4820                       mov       rcx, qword ptr [rax + 0x20]
007D1FFA  482bca                         sub       rcx, rdx
007D1FFD  4a8d1401                       lea       rdx, [rcx + r8]
007D2001  48895348                       mov       qword ptr [rbx + 0x48], rdx
007D2005  493bd0                         cmp       rdx, r8
007D2008  7219                           jb        0x7d2023
007D200A  488d4201                       lea       rax, [rdx + 1]
007D200E  483b4358                       cmp       rax, qword ptr [rbx + 0x58]
007D2012  770f                           ja        0x7d2023
007D2014  0fb602                         movzx     eax, byte ptr [rdx]
007D2017  8887f0010000                   mov       byte ptr [rdi + 0x1f0], al
007D201D  48ff4348                       inc       qword ptr [rbx + 0x48]
007D2021  eb2d                           jmp       0x7d2050
007D2023  41b801000000                   mov       r8d, 1
007D2029  488d97f0010000                 lea       rdx, [rdi + 0x1f0]
007D2030  488d4b48                       lea       rcx, [rbx + 0x48]
007D2034  e827651b00                     call      0x988560
007D2039  eb15                           jmp       0x7d2050
007D203B  488b4520                       mov       rax, qword ptr [rbp + 0x20]
007D203F  4885c0                         test      rax, rax
007D2042  740c                           je        0x7d2050
007D2044  488bd3                         mov       rdx, rbx
007D2047  488d8ff0010000                 lea       rcx, [rdi + 0x1f0]
007D204E  ffd0                           call      rax
007D2050  488bcb                         mov       rcx, rbx
007D2053  e858114000                     call      0xbd31b0
007D2058  8b8730010000                   mov       eax, dword ptr [rdi + 0x130]
007D205E  4c8d4d28                       lea       r9, [rbp + 0x28]
007D2062  4c8b05a7873601                 mov       r8, qword ptr [rip + 0x13687a7] ; RIP_RVA=0x1b3a810
007D2069  488d1590383701                 lea       rdx, [rip + 0x1373890] ; RIP_RVA=0x1b45900
007D2070  c1e806                         shr       eax, 6
007D2073  488bcb                         mov       rcx, rbx
007D2076  2403                           and       al, 3
007D2078  c644242000                     mov       byte ptr [rsp + 0x20], 0
007D207D  884520                         mov       byte ptr [rbp + 0x20], al
007D2080  e84b044000                     call      0xbd24d0
007D2085  85c0                           test      eax, eax
007D2087  746f                           je        0x7d20f8
007D2089  83f801                         cmp       eax, 1
007D208C  7c50                           jl        0x7d20de
007D208E  488b83d0000000                 mov       rax, qword ptr [rbx + 0xd0]
007D2095  48635368                       movsxd    rdx, dword ptr [rbx + 0x68]
007D2099  480faf5370                     imul      rdx, qword ptr [rbx + 0x70]
007D209E  488b4020                       mov       rax, qword ptr [rax + 0x20]
007D20A2  482bc2                         sub       rax, rdx
007D20A5  48034350                       add       rax, qword ptr [rbx + 0x50]
007D20A9  48894348                       mov       qword ptr [rbx + 0x48], rax
007D20AD  483b4350                       cmp       rax, qword ptr [rbx + 0x50]
007D20B1  7216                           jb        0x7d20c9
007D20B3  488d4801                       lea       rcx, [rax + 1]
007D20B7  483b4b58                       cmp       rcx, qword ptr [rbx + 0x58]
007D20BB  770c                           ja        0x7d20c9
007D20BD  0fb600                         movzx     eax, byte ptr [rax]
007D20C0  884520                         mov       byte ptr [rbp + 0x20], al
007D20C3  48894b48                       mov       qword ptr [rbx + 0x48], rcx
007D20C7  eb27                           jmp       0x7d20f0
007D20C9  41b801000000                   mov       r8d, 1
007D20CF  488d5520                       lea       rdx, [rbp + 0x20]
007D20D3  488d4b48                       lea       rcx, [rbx + 0x48]
007D20D7  e884641b00                     call      0x988560
007D20DC  eb12                           jmp       0x7d20f0
007D20DE  488b4528                       mov       rax, qword ptr [rbp + 0x28]
007D20E2  4885c0                         test      rax, rax
007D20E5  7409                           je        0x7d20f0
007D20E7  488bd3                         mov       rdx, rbx
007D20EA  488d4d20                       lea       rcx, [rbp + 0x20]
007D20EE  ffd0                           call      rax
007D20F0  488bcb                         mov       rcx, rbx
007D20F3  e8b8104000                     call      0xbd31b0
007D20F8  0fb64520                       movzx     eax, byte ptr [rbp + 0x20]
007D20FC  4c8d4d28                       lea       r9, [rbp + 0x28]
007D2100  c1e006                         shl       eax, 6
007D2103  488d15ded93601                 lea       rdx, [rip + 0x136d9de] ; RIP_RVA=0x1b3fae8
007D210A  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D2110  488bcb                         mov       rcx, rbx
007D2113  25c0000000                     and       eax, 0xc0
007D2118  c644242000                     mov       byte ptr [rsp + 0x20], 0
007D211D  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D2123  898730010000                   mov       dword ptr [rdi + 0x130], eax
007D2129  4c8b05e0863601                 mov       r8, qword ptr [rip + 0x13686e0] ; RIP_RVA=0x1b3a810
007D2130  c1e808                         shr       eax, 8
007D2133  2401                           and       al, 1
007D2135  884520                         mov       byte ptr [rbp + 0x20], al
007D2138  e893034000                     call      0xbd24d0
007D213D  85c0                           test      eax, eax
007D213F  746f                           je        0x7d21b0
007D2141  83f801                         cmp       eax, 1
007D2144  7c50                           jl        0x7d2196
007D2146  488b83d0000000                 mov       rax, qword ptr [rbx + 0xd0]
007D214D  48635368                       movsxd    rdx, dword ptr [rbx + 0x68]
007D2151  480faf5370                     imul      rdx, qword ptr [rbx + 0x70]
007D2156  488b4020                       mov       rax, qword ptr [rax + 0x20]
007D215A  482bc2                         sub       rax, rdx
007D215D  48034350                       add       rax, qword ptr [rbx + 0x50]
007D2161  48894348                       mov       qword ptr [rbx + 0x48], rax
007D2165  483b4350                       cmp       rax, qword ptr [rbx + 0x50]
007D2169  7216                           jb        0x7d2181
007D216B  488d4801                       lea       rcx, [rax + 1]
007D216F  483b4b58                       cmp       rcx, qword ptr [rbx + 0x58]
007D2173  770c                           ja        0x7d2181
007D2175  0fb600                         movzx     eax, byte ptr [rax]
007D2178  884520                         mov       byte ptr [rbp + 0x20], al
007D217B  48894b48                       mov       qword ptr [rbx + 0x48], rcx
007D217F  eb27                           jmp       0x7d21a8
007D2181  41b801000000                   mov       r8d, 1
007D2187  488d5520                       lea       rdx, [rbp + 0x20]
007D218B  488d4b48                       lea       rcx, [rbx + 0x48]
007D218F  e8cc631b00                     call      0x988560
007D2194  eb12                           jmp       0x7d21a8
007D2196  488b4528                       mov       rax, qword ptr [rbp + 0x28]
007D219A  4885c0                         test      rax, rax
007D219D  7409                           je        0x7d21a8
007D219F  488bd3                         mov       rdx, rbx
007D21A2  488d4d20                       lea       rcx, [rbp + 0x20]
007D21A6  ffd0                           call      rax
007D21A8  488bcb                         mov       rcx, rbx
007D21AB  e800104000                     call      0xbd31b0
007D21B0  0fb64520                       movzx     eax, byte ptr [rbp + 0x20]
007D21B4  4c8d4d28                       lea       r9, [rbp + 0x28]
007D21B8  c1e008                         shl       eax, 8
007D21BB  488d154e373701                 lea       rdx, [rip + 0x137374e] ; RIP_RVA=0x1b45910
007D21C2  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D21C8  488bcb                         mov       rcx, rbx
007D21CB  2500010000                     and       eax, 0x100
007D21D0  c644242000                     mov       byte ptr [rsp + 0x20], 0
007D21D5  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D21DB  898730010000                   mov       dword ptr [rdi + 0x130], eax
007D21E1  4c8b0528863601                 mov       r8, qword ptr [rip + 0x1368628] ; RIP_RVA=0x1b3a810
007D21E8  c1e816                         shr       eax, 0x16
007D21EB  2401                           and       al, 1
007D21ED  884520                         mov       byte ptr [rbp + 0x20], al
007D21F0  e8db024000                     call      0xbd24d0
007D21F5  85c0                           test      eax, eax
007D21F7  746f                           je        0x7d2268
007D21F9  83f801                         cmp       eax, 1
007D21FC  7c50                           jl        0x7d224e
007D21FE  488b83d0000000                 mov       rax, qword ptr [rbx + 0xd0]
007D2205  48635368                       movsxd    rdx, dword ptr [rbx + 0x68]
007D2209  480faf5370                     imul      rdx, qword ptr [rbx + 0x70]
007D220E  488b4020                       mov       rax, qword ptr [rax + 0x20]
007D2212  482bc2                         sub       rax, rdx
007D2215  48034350                       add       rax, qword ptr [rbx + 0x50]
007D2219  48894348                       mov       qword ptr [rbx + 0x48], rax
007D221D  483b4350                       cmp       rax, qword ptr [rbx + 0x50]
007D2221  7216                           jb        0x7d2239
007D2223  488d4801                       lea       rcx, [rax + 1]
007D2227  483b4b58                       cmp       rcx, qword ptr [rbx + 0x58]
007D222B  770c                           ja        0x7d2239
007D222D  0fb600                         movzx     eax, byte ptr [rax]
007D2230  884520                         mov       byte ptr [rbp + 0x20], al
007D2233  48894b48                       mov       qword ptr [rbx + 0x48], rcx
007D2237  eb27                           jmp       0x7d2260
007D2239  41b801000000                   mov       r8d, 1
007D223F  488d5520                       lea       rdx, [rbp + 0x20]
007D2243  488d4b48                       lea       rcx, [rbx + 0x48]
007D2247  e814631b00                     call      0x988560
007D224C  eb12                           jmp       0x7d2260
007D224E  488b4528                       mov       rax, qword ptr [rbp + 0x28]
007D2252  4885c0                         test      rax, rax
007D2255  7409                           je        0x7d2260
007D2257  488bd3                         mov       rdx, rbx
007D225A  488d4d20                       lea       rcx, [rbp + 0x20]
007D225E  ffd0                           call      rax
007D2260  488bcb                         mov       rcx, rbx
007D2263  e8480f4000                     call      0xbd31b0
007D2268  0fb64520                       movzx     eax, byte ptr [rbp + 0x20]
007D226C  4c8d4d28                       lea       r9, [rbp + 0x28]
007D2270  c1e016                         shl       eax, 0x16
007D2273  488d1566723601                 lea       rdx, [rip + 0x1367266] ; RIP_RVA=0x1b394e0
007D227A  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D2280  488bcb                         mov       rcx, rbx
007D2283  2500004000                     and       eax, 0x400000
007D2288  c644242000                     mov       byte ptr [rsp + 0x20], 0
007D228D  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D2293  898730010000                   mov       dword ptr [rdi + 0x130], eax
007D2299  4c8b0570853601                 mov       r8, qword ptr [rip + 0x1368570] ; RIP_RVA=0x1b3a810
007D22A0  c1e80a                         shr       eax, 0xa
007D22A3  2403                           and       al, 3
007D22A5  884520                         mov       byte ptr [rbp + 0x20], al
007D22A8  e823024000                     call      0xbd24d0
007D22AD  85c0                           test      eax, eax
007D22AF  746f                           je        0x7d2320
007D22B1  83f801                         cmp       eax, 1
007D22B4  7c50                           jl        0x7d2306
007D22B6  488b83d0000000                 mov       rax, qword ptr [rbx + 0xd0]
007D22BD  48635368                       movsxd    rdx, dword ptr [rbx + 0x68]
007D22C1  480faf5370                     imul      rdx, qword ptr [rbx + 0x70]
007D22C6  488b4020                       mov       rax, qword ptr [rax + 0x20]
007D22CA  482bc2                         sub       rax, rdx
007D22CD  48034350                       add       rax, qword ptr [rbx + 0x50]
007D22D1  48894348                       mov       qword ptr [rbx + 0x48], rax
007D22D5  483b4350                       cmp       rax, qword ptr [rbx + 0x50]
007D22D9  7216                           jb        0x7d22f1
007D22DB  488d4801                       lea       rcx, [rax + 1]
007D22DF  483b4b58                       cmp       rcx, qword ptr [rbx + 0x58]
007D22E3  770c                           ja        0x7d22f1
007D22E5  0fb600                         movzx     eax, byte ptr [rax]
007D22E8  884520                         mov       byte ptr [rbp + 0x20], al
007D22EB  48894b48                       mov       qword ptr [rbx + 0x48], rcx
007D22EF  eb27                           jmp       0x7d2318
007D22F1  41b801000000                   mov       r8d, 1
007D22F7  488d5520                       lea       rdx, [rbp + 0x20]
007D22FB  488d4b48                       lea       rcx, [rbx + 0x48]
007D22FF  e85c621b00                     call      0x988560
007D2304  eb12                           jmp       0x7d2318
007D2306  488b4528                       mov       rax, qword ptr [rbp + 0x28]
007D230A  4885c0                         test      rax, rax
007D230D  7409                           je        0x7d2318
007D230F  488bd3                         mov       rdx, rbx
007D2312  488d4d20                       lea       rcx, [rbp + 0x20]
007D2316  ffd0                           call      rax
007D2318  488bcb                         mov       rcx, rbx
007D231B  e8900e4000                     call      0xbd31b0
007D2320  0fb64520                       movzx     eax, byte ptr [rbp + 0x20]
007D2324  4c8d4d28                       lea       r9, [rbp + 0x28]
007D2328  c1e00a                         shl       eax, 0xa
007D232B  488d15f6353701                 lea       rdx, [rip + 0x13735f6] ; RIP_RVA=0x1b45928
007D2332  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D2338  488bcb                         mov       rcx, rbx
007D233B  25000c0000                     and       eax, 0xc00
007D2340  c644242000                     mov       byte ptr [rsp + 0x20], 0
007D2345  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D234B  898730010000                   mov       dword ptr [rdi + 0x130], eax
007D2351  4c8b05b8843601                 mov       r8, qword ptr [rip + 0x13684b8] ; RIP_RVA=0x1b3a810
007D2358  c1e810                         shr       eax, 0x10
007D235B  2407                           and       al, 7
007D235D  884520                         mov       byte ptr [rbp + 0x20], al
007D2360  e86b014000                     call      0xbd24d0
007D2365  85c0                           test      eax, eax
007D2367  746f                           je        0x7d23d8
007D2369  83f801                         cmp       eax, 1
007D236C  7c50                           jl        0x7d23be
007D236E  488b83d0000000                 mov       rax, qword ptr [rbx + 0xd0]
007D2375  48635368                       movsxd    rdx, dword ptr [rbx + 0x68]
007D2379  480faf5370                     imul      rdx, qword ptr [rbx + 0x70]
007D237E  488b4020                       mov       rax, qword ptr [rax + 0x20]
007D2382  482bc2                         sub       rax, rdx
007D2385  48034350                       add       rax, qword ptr [rbx + 0x50]
007D2389  48894348                       mov       qword ptr [rbx + 0x48], rax
007D238D  483b4350                       cmp       rax, qword ptr [rbx + 0x50]
007D2391  7216                           jb        0x7d23a9
007D2393  488d4801                       lea       rcx, [rax + 1]
007D2397  483b4b58                       cmp       rcx, qword ptr [rbx + 0x58]
007D239B  770c                           ja        0x7d23a9
007D239D  0fb600                         movzx     eax, byte ptr [rax]
007D23A0  884520                         mov       byte ptr [rbp + 0x20], al
007D23A3  48894b48                       mov       qword ptr [rbx + 0x48], rcx
007D23A7  eb27                           jmp       0x7d23d0
007D23A9  41b801000000                   mov       r8d, 1
007D23AF  488d5520                       lea       rdx, [rbp + 0x20]
007D23B3  488d4b48                       lea       rcx, [rbx + 0x48]
007D23B7  e8a4611b00                     call      0x988560
007D23BC  eb12                           jmp       0x7d23d0
007D23BE  488b4528                       mov       rax, qword ptr [rbp + 0x28]
007D23C2  4885c0                         test      rax, rax
007D23C5  7409                           je        0x7d23d0
007D23C7  488bd3                         mov       rdx, rbx
007D23CA  488d4d20                       lea       rcx, [rbp + 0x20]
007D23CE  ffd0                           call      rax
007D23D0  488bcb                         mov       rcx, rbx
007D23D3  e8d80d4000                     call      0xbd31b0
007D23D8  0fb64520                       movzx     eax, byte ptr [rbp + 0x20]
007D23DC  4c8d4d28                       lea       r9, [rbp + 0x28]
007D23E0  c1e010                         shl       eax, 0x10
007D23E3  488d1556353701                 lea       rdx, [rip + 0x1373556] ; RIP_RVA=0x1b45940
007D23EA  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D23F0  488bcb                         mov       rcx, rbx
007D23F3  2500000700                     and       eax, 0x70000
007D23F8  c644242000                     mov       byte ptr [rsp + 0x20], 0
007D23FD  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D2403  898730010000                   mov       dword ptr [rdi + 0x130], eax
007D2409  4c8b0500843601                 mov       r8, qword ptr [rip + 0x1368400] ; RIP_RVA=0x1b3a810
007D2410  c1e80e                         shr       eax, 0xe
007D2413  2403                           and       al, 3
007D2415  884520                         mov       byte ptr [rbp + 0x20], al
007D2418  e8b3004000                     call      0xbd24d0
007D241D  85c0                           test      eax, eax
007D241F  746f                           je        0x7d2490
007D2421  83f801                         cmp       eax, 1
007D2424  7c50                           jl        0x7d2476
007D2426  488b83d0000000                 mov       rax, qword ptr [rbx + 0xd0]
007D242D  48635368                       movsxd    rdx, dword ptr [rbx + 0x68]
007D2431  480faf5370                     imul      rdx, qword ptr [rbx + 0x70]
007D2436  488b4020                       mov       rax, qword ptr [rax + 0x20]
007D243A  482bc2                         sub       rax, rdx
007D243D  48034350                       add       rax, qword ptr [rbx + 0x50]
007D2441  48894348                       mov       qword ptr [rbx + 0x48], rax
007D2445  483b4350                       cmp       rax, qword ptr [rbx + 0x50]
007D2449  7216                           jb        0x7d2461
007D244B  488d4801                       lea       rcx, [rax + 1]
007D244F  483b4b58                       cmp       rcx, qword ptr [rbx + 0x58]
007D2453  770c                           ja        0x7d2461
007D2455  0fb600                         movzx     eax, byte ptr [rax]
007D2458  884520                         mov       byte ptr [rbp + 0x20], al
007D245B  48894b48                       mov       qword ptr [rbx + 0x48], rcx
007D245F  eb27                           jmp       0x7d2488
007D2461  41b801000000                   mov       r8d, 1
007D2467  488d5520                       lea       rdx, [rbp + 0x20]
007D246B  488d4b48                       lea       rcx, [rbx + 0x48]
007D246F  e8ec601b00                     call      0x988560
007D2474  eb12                           jmp       0x7d2488
007D2476  488b4528                       mov       rax, qword ptr [rbp + 0x28]
007D247A  4885c0                         test      rax, rax
007D247D  7409                           je        0x7d2488
007D247F  488bd3                         mov       rdx, rbx
007D2482  488d4d20                       lea       rcx, [rbp + 0x20]
007D2486  ffd0                           call      rax
007D2488  488bcb                         mov       rcx, rbx
007D248B  e8200d4000                     call      0xbd31b0
007D2490  0fb64520                       movzx     eax, byte ptr [rbp + 0x20]
007D2494  4c8d4d28                       lea       r9, [rbp + 0x28]
007D2498  c1e00e                         shl       eax, 0xe
007D249B  488d15b6343701                 lea       rdx, [rip + 0x13734b6] ; RIP_RVA=0x1b45958
007D24A2  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D24A8  488bcb                         mov       rcx, rbx
007D24AB  2500c00000                     and       eax, 0xc000
007D24B0  c644242000                     mov       byte ptr [rsp + 0x20], 0
007D24B5  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D24BB  898730010000                   mov       dword ptr [rdi + 0x130], eax
007D24C1  4c8b0548833601                 mov       r8, qword ptr [rip + 0x1368348] ; RIP_RVA=0x1b3a810
007D24C8  c1e813                         shr       eax, 0x13
007D24CB  2403                           and       al, 3
007D24CD  884520                         mov       byte ptr [rbp + 0x20], al
007D24D0  e8fbff3f00                     call      0xbd24d0
007D24D5  85c0                           test      eax, eax
007D24D7  746f                           je        0x7d2548
007D24D9  83f801                         cmp       eax, 1
007D24DC  7c50                           jl        0x7d252e
007D24DE  488b83d0000000                 mov       rax, qword ptr [rbx + 0xd0]
007D24E5  48635368                       movsxd    rdx, dword ptr [rbx + 0x68]
007D24E9  480faf5370                     imul      rdx, qword ptr [rbx + 0x70]
007D24EE  488b4020                       mov       rax, qword ptr [rax + 0x20]
007D24F2  482bc2                         sub       rax, rdx
007D24F5  48034350                       add       rax, qword ptr [rbx + 0x50]
007D24F9  48894348                       mov       qword ptr [rbx + 0x48], rax
007D24FD  483b4350                       cmp       rax, qword ptr [rbx + 0x50]
007D2501  7216                           jb        0x7d2519
007D2503  488d4801                       lea       rcx, [rax + 1]
007D2507  483b4b58                       cmp       rcx, qword ptr [rbx + 0x58]
007D250B  770c                           ja        0x7d2519
007D250D  0fb600                         movzx     eax, byte ptr [rax]
007D2510  884520                         mov       byte ptr [rbp + 0x20], al
007D2513  48894b48                       mov       qword ptr [rbx + 0x48], rcx
007D2517  eb27                           jmp       0x7d2540
007D2519  41b801000000                   mov       r8d, 1
007D251F  488d5520                       lea       rdx, [rbp + 0x20]
007D2523  488d4b48                       lea       rcx, [rbx + 0x48]
007D2527  e834601b00                     call      0x988560
007D252C  eb12                           jmp       0x7d2540
007D252E  488b4528                       mov       rax, qword ptr [rbp + 0x28]
007D2532  4885c0                         test      rax, rax
007D2535  7409                           je        0x7d2540
007D2537  488bd3                         mov       rdx, rbx
007D253A  488d4d20                       lea       rcx, [rbp + 0x20]
007D253E  ffd0                           call      rax
007D2540  488bcb                         mov       rcx, rbx
007D2543  e8680c4000                     call      0xbd31b0
007D2548  0fb64520                       movzx     eax, byte ptr [rbp + 0x20]
007D254C  4c8d4d28                       lea       r9, [rbp + 0x28]
007D2550  c1e013                         shl       eax, 0x13
007D2553  488d1516343701                 lea       rdx, [rip + 0x1373416] ; RIP_RVA=0x1b45970
007D255A  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D2560  488bcb                         mov       rcx, rbx
007D2563  2500001800                     and       eax, 0x180000
007D2568  c644242000                     mov       byte ptr [rsp + 0x20], 0
007D256D  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D2573  898730010000                   mov       dword ptr [rdi + 0x130], eax
007D2579  4c8b0590823601                 mov       r8, qword ptr [rip + 0x1368290] ; RIP_RVA=0x1b3a810
007D2580  c1e815                         shr       eax, 0x15
007D2583  2401                           and       al, 1
007D2585  884520                         mov       byte ptr [rbp + 0x20], al
007D2588  e843ff3f00                     call      0xbd24d0
007D258D  85c0                           test      eax, eax
007D258F  746f                           je        0x7d2600
007D2591  83f801                         cmp       eax, 1
007D2594  7c50                           jl        0x7d25e6
007D2596  488b83d0000000                 mov       rax, qword ptr [rbx + 0xd0]
007D259D  48635368                       movsxd    rdx, dword ptr [rbx + 0x68]
007D25A1  480faf5370                     imul      rdx, qword ptr [rbx + 0x70]
007D25A6  488b4020                       mov       rax, qword ptr [rax + 0x20]
007D25AA  482bc2                         sub       rax, rdx
007D25AD  48034350                       add       rax, qword ptr [rbx + 0x50]
007D25B1  48894348                       mov       qword ptr [rbx + 0x48], rax
007D25B5  483b4350                       cmp       rax, qword ptr [rbx + 0x50]
007D25B9  7216                           jb        0x7d25d1
007D25BB  488d4801                       lea       rcx, [rax + 1]
007D25BF  483b4b58                       cmp       rcx, qword ptr [rbx + 0x58]
007D25C3  770c                           ja        0x7d25d1
007D25C5  0fb600                         movzx     eax, byte ptr [rax]
007D25C8  884520                         mov       byte ptr [rbp + 0x20], al
007D25CB  48894b48                       mov       qword ptr [rbx + 0x48], rcx
007D25CF  eb27                           jmp       0x7d25f8
007D25D1  41b801000000                   mov       r8d, 1
007D25D7  488d5520                       lea       rdx, [rbp + 0x20]
007D25DB  488d4b48                       lea       rcx, [rbx + 0x48]
007D25DF  e87c5f1b00                     call      0x988560
007D25E4  eb12                           jmp       0x7d25f8
007D25E6  488b4528                       mov       rax, qword ptr [rbp + 0x28]
007D25EA  4885c0                         test      rax, rax
007D25ED  7409                           je        0x7d25f8
007D25EF  488bd3                         mov       rdx, rbx
007D25F2  488d4d20                       lea       rcx, [rbp + 0x20]
007D25F6  ffd0                           call      rax
007D25F8  488bcb                         mov       rcx, rbx
007D25FB  e8b00b4000                     call      0xbd31b0
007D2600  81a730010000ffffdfff           and       dword ptr [rdi + 0x130], 0xffdfffff
007D260A  4c8d4d20                       lea       r9, [rbp + 0x20]
007D260E  0fb64520                       movzx     eax, byte ptr [rbp + 0x20]
007D2612  488d15272d3601                 lea       rdx, [rip + 0x1362d27] ; RIP_RVA=0x1b35340
007D2619  83e001                         and       eax, 1
007D261C  c644242000                     mov       byte ptr [rsp + 0x20], 0
007D2621  c1e015                         shl       eax, 0x15
007D2624  488bcb                         mov       rcx, rbx
007D2627  098730010000                   or        dword ptr [rdi + 0x130], eax
007D262D  4c8b05e4813601                 mov       r8, qword ptr [rip + 0x13681e4] ; RIP_RVA=0x1b3a818
007D2634  e897fe3f00                     call      0xbd24d0
007D2639  85c0                           test      eax, eax
007D263B  0f8493000000                   je        0x7d26d4
007D2641  83f801                         cmp       eax, 1
007D2644  7c71                           jl        0x7d26b7
007D2646  488b83d0000000                 mov       rax, qword ptr [rbx + 0xd0]
007D264D  4c8b4350                       mov       r8, qword ptr [rbx + 0x50]
007D2651  48635368                       movsxd    rdx, dword ptr [rbx + 0x68]
007D2655  480faf5370                     imul      rdx, qword ptr [rbx + 0x70]
007D265A  488b4820                       mov       rcx, qword ptr [rax + 0x20]
007D265E  482bca                         sub       rcx, rdx
007D2661  4a8d1401                       lea       rdx, [rcx + r8]
007D2665  48895348                       mov       qword ptr [rbx + 0x48], rdx
007D2669  493bd0                         cmp       rdx, r8
007D266C  7219                           jb        0x7d2687
007D266E  488d4204                       lea       rax, [rdx + 4]
007D2672  483b4358                       cmp       rax, qword ptr [rbx + 0x58]
007D2676  770f                           ja        0x7d2687
007D2678  8b02                           mov       eax, dword ptr [rdx]
007D267A  898734010000                   mov       dword ptr [rdi + 0x134], eax
007D2680  4883434804                     add       qword ptr [rbx + 0x48], 4
007D2685  eb16                           jmp       0x7d269d
007D2687  41b804000000                   mov       r8d, 4
007D268D  488d9734010000                 lea       rdx, [rdi + 0x134]
007D2694  488d4b48                       lea       rcx, [rbx + 0x48]
007D2698  e8c35e1b00                     call      0x988560
007D269D  8b03                           mov       eax, dword ptr [rbx]
007D269F  48c1e809                       shr       rax, 9
007D26A3  a801                           test      al, 1
007D26A5  7425                           je        0x7d26cc
007D26A7  8b8734010000                   mov       eax, dword ptr [rdi + 0x134]
007D26AD  0fc8                           bswap     eax
007D26AF  898734010000                   mov       dword ptr [rdi + 0x134], eax
007D26B5  eb15                           jmp       0x7d26cc
007D26B7  488b4520                       mov       rax, qword ptr [rbp + 0x20]
007D26BB  4885c0                         test      rax, rax
007D26BE  740c                           je        0x7d26cc
007D26C0  488bd3                         mov       rdx, rbx
007D26C3  488d8f34010000                 lea       rcx, [rdi + 0x134]
007D26CA  ffd0                           call      rax
007D26CC  488bcb                         mov       rcx, rbx
007D26CF  e8dc0a4000                     call      0xbd31b0
007D26D4  488d9744010000                 lea       rdx, [rdi + 0x144]
007D26DB  4533c9                         xor       r9d, r9d
007D26DE  4c8d05a3323701                 lea       r8, [rip + 0x13732a3] ; RIP_RVA=0x1b45988
007D26E5  488bcb                         mov       rcx, rbx
007D26E8  e8b34f92ff                     call      0xf76a0
007D26ED  488d972c010000                 lea       rdx, [rdi + 0x12c]
007D26F4  41b901008000                   mov       r9d, 0x800001
007D26FA  4c8d059f323701                 lea       r8, [rip + 0x137329f] ; RIP_RVA=0x1b459a0
007D2701  488bcb                         mov       rcx, rbx
007D2704  e867d89aff                     call      0x17ff70
007D2709  488d972e010000                 lea       rdx, [rdi + 0x12e]
007D2710  41b901008000                   mov       r9d, 0x800001
007D2716  4c8d0593323701                 lea       r8, [rip + 0x1373293] ; RIP_RVA=0x1b459b0
007D271D  488bcb                         mov       rcx, rbx
007D2720  e84bd89aff                     call      0x17ff70
007D2725  488d970c010000                 lea       rdx, [rdi + 0x10c]
007D272C  41b901008000                   mov       r9d, 0x800001
007D2732  4c8d058f323701                 lea       r8, [rip + 0x137328f] ; RIP_RVA=0x1b459c8
007D2739  488bcb                         mov       rcx, rbx
007D273C  e87f409bff                     call      0x1867c0
007D2741  488d971c010000                 lea       rdx, [rdi + 0x11c]
007D2748  41b901008000                   mov       r9d, 0x800001
007D274E  4c8d058b323701                 lea       r8, [rip + 0x137328b] ; RIP_RVA=0x1b459e0
007D2755  488bcb                         mov       rcx, rbx
007D2758  e863409bff                     call      0x1867c0
007D275D  4c8b05bc803601                 mov       r8, qword ptr [rip + 0x13680bc] ; RIP_RVA=0x1b3a820
007D2764  4c8d4d20                       lea       r9, [rbp + 0x20]
007D2768  488d1591323701                 lea       rdx, [rip + 0x1373291] ; RIP_RVA=0x1b45a00
007D276F  c644242001                     mov       byte ptr [rsp + 0x20], 1
007D2774  488bcb                         mov       rcx, rbx
007D2777  e854fd3f00                     call      0xbd24d0
007D277C  85c0                           test      eax, eax
007D277E  7436                           je        0x7d27b6
007D2780  83f801                         cmp       eax, 1
007D2783  7c14                           jl        0x7d2799
007D2785  4533c0                         xor       r8d, r8d
007D2788  488d9788010000                 lea       rdx, [rdi + 0x188]
007D278F  488bcb                         mov       rcx, rbx
007D2792  e869100000                     call      0x7d3800
007D2797  eb15                           jmp       0x7d27ae
007D2799  488b4520                       mov       rax, qword ptr [rbp + 0x20]
007D279D  4885c0                         test      rax, rax
007D27A0  740c                           je        0x7d27ae
007D27A2  488bd3                         mov       rdx, rbx
007D27A5  488d8f88010000                 lea       rcx, [rdi + 0x188]
007D27AC  ffd0                           call      rax
007D27AE  488bcb                         mov       rcx, rbx
007D27B1  e8fa094000                     call      0xbd31b0
007D27B6  4c8d4d20                       lea       r9, [rbp + 0x20]
007D27BA  c644242001                     mov       byte ptr [rsp + 0x20], 1
007D27BF  4c8d05322f3701                 lea       r8, [rip + 0x1372f32] ; RIP_RVA=0x1b456f8
007D27C6  488bcb                         mov       rcx, rbx
007D27C9  488d1540323701                 lea       rdx, [rip + 0x1373240] ; RIP_RVA=0x1b45a10
007D27D0  e8fbfc3f00                     call      0xbd24d0
007D27D5  85c0                           test      eax, eax
007D27D7  7456                           je        0x7d282f
007D27D9  83f801                         cmp       eax, 1
007D27DC  7c34                           jl        0x7d2812
007D27DE  4533c9                         xor       r9d, r9d
007D27E1  4c8d05e8323701                 lea       r8, [rip + 0x13732e8] ; RIP_RVA=0x1b45ad0
007D27E8  488d9700010000                 lea       rdx, [rdi + 0x100]
007D27EF  488bcb                         mov       rcx, rbx
007D27F2  e879d79aff                     call      0x17ff70
007D27F7  488d9702010000                 lea       rdx, [rdi + 0x102]
007D27FE  4533c9                         xor       r9d, r9d
007D2801  4c8d05d8323701                 lea       r8, [rip + 0x13732d8] ; RIP_RVA=0x1b45ae0
007D2808  488bcb                         mov       rcx, rbx
007D280B  e860d79aff                     call      0x17ff70
007D2810  eb15                           jmp       0x7d2827
007D2812  488b4520                       mov       rax, qword ptr [rbp + 0x20]
007D2816  4885c0                         test      rax, rax
007D2819  740c                           je        0x7d2827
007D281B  488bd3                         mov       rdx, rbx
007D281E  488d8f00010000                 lea       rcx, [rdi + 0x100]
007D2825  ffd0                           call      rax
007D2827  488bcb                         mov       rcx, rbx
007D282A  e881094000                     call      0xbd31b0
007D282F  4c8d4d20                       lea       r9, [rbp + 0x20]
007D2833  c644242001                     mov       byte ptr [rsp + 0x20], 1
007D2838  4c8d0579583101                 lea       r8, [rip + 0x1315879] ; RIP_RVA=0x1ae80b8
007D283F  488bcb                         mov       rcx, rbx
007D2842  488d15df313701                 lea       rdx, [rip + 0x13731df] ; RIP_RVA=0x1b45a28
007D2849  e882fc3f00                     call      0xbd24d0
007D284E  85c0                           test      eax, eax
007D2850  7433                           je        0x7d2885
007D2852  83f801                         cmp       eax, 1
007D2855  7c11                           jl        0x7d2868
007D2857  488bd3                         mov       rdx, rbx
007D285A  488d8fd0010000                 lea       rcx, [rdi + 0x1d0]
007D2861  e80a6d92ff                     call      0xf9570
007D2866  eb15                           jmp       0x7d287d
007D2868  488b4520                       mov       rax, qword ptr [rbp + 0x20]
007D286C  4885c0                         test      rax, rax
007D286F  740c                           je        0x7d287d
007D2871  488bd3                         mov       rdx, rbx
007D2874  488d8fd0010000                 lea       rcx, [rdi + 0x1d0]
007D287B  ffd0                           call      rax
007D287D  488bcb                         mov       rcx, rbx
007D2880  e82b094000                     call      0xbd31b0
007D2885  4c8d4d20                       lea       r9, [rbp + 0x20]
007D2889  c644242001                     mov       byte ptr [rsp + 0x20], 1
007D288E  4c8d0523583101                 lea       r8, [rip + 0x1315823] ; RIP_RVA=0x1ae80b8
007D2895  488bcb                         mov       rcx, rbx
007D2898  488d15a1313701                 lea       rdx, [rip + 0x13731a1] ; RIP_RVA=0x1b45a40
007D289F  e82cfc3f00                     call      0xbd24d0
007D28A4  85c0                           test      eax, eax
007D28A6  7433                           je        0x7d28db
007D28A8  83f801                         cmp       eax, 1
007D28AB  7c11                           jl        0x7d28be
007D28AD  488bd3                         mov       rdx, rbx
007D28B0  488d8fd4010000                 lea       rcx, [rdi + 0x1d4]
007D28B7  e8b46c92ff                     call      0xf9570
007D28BC  eb15                           jmp       0x7d28d3
007D28BE  488b4520                       mov       rax, qword ptr [rbp + 0x20]
007D28C2  4885c0                         test      rax, rax
007D28C5  740c                           je        0x7d28d3
007D28C7  488bd3                         mov       rdx, rbx
007D28CA  488d8fd4010000                 lea       rcx, [rdi + 0x1d4]
007D28D1  ffd0                           call      rax
007D28D3  488bcb                         mov       rcx, rbx
007D28D6  e8d5084000                     call      0xbd31b0
007D28DB  4c8d4d20                       lea       r9, [rbp + 0x20]
007D28DF  c644242001                     mov       byte ptr [rsp + 0x20], 1
007D28E4  4c8d0595573001                 lea       r8, [rip + 0x1305795] ; RIP_RVA=0x1ad8080
007D28EB  488bcb                         mov       rcx, rbx
007D28EE  488d155b313701                 lea       rdx, [rip + 0x137315b] ; RIP_RVA=0x1b45a50
007D28F5  e8d6fb3f00                     call      0xbd24d0
007D28FA  85c0                           test      eax, eax
007D28FC  7433                           je        0x7d2931
007D28FE  83f801                         cmp       eax, 1
007D2901  7c11                           jl        0x7d2914
007D2903  488bd3                         mov       rdx, rbx
007D2906  488d8fdc010000                 lea       rcx, [rdi + 0x1dc]
007D290D  e85e6c92ff                     call      0xf9570
007D2912  eb15                           jmp       0x7d2929
007D2914  488b4520                       mov       rax, qword ptr [rbp + 0x20]
007D2918  4885c0                         test      rax, rax
007D291B  740c                           je        0x7d2929
007D291D  488bd3                         mov       rdx, rbx
007D2920  488d8fdc010000                 lea       rcx, [rdi + 0x1dc]
007D2927  ffd0                           call      rax
007D2929  488bcb                         mov       rcx, rbx
007D292C  e87f084000                     call      0xbd31b0
007D2931  488d97f4010000                 lea       rdx, [rdi + 0x1f4]
007D2938  41b901008000                   mov       r9d, 0x800001
007D293E  4c8d0543c13001                 lea       r8, [rip + 0x130c143] ; RIP_RVA=0x1adea88
007D2945  488bcb                         mov       rcx, rbx
007D2948  e8534d92ff                     call      0xf76a0
007D294D  488d97fc010000                 lea       rdx, [rdi + 0x1fc]
007D2954  41b901000000                   mov       r9d, 1
007D295A  4c8d056fef3501                 lea       r8, [rip + 0x135ef6f] ; RIP_RVA=0x1b318d0
007D2961  488bcb                         mov       rcx, rbx
007D2964  e867f096ff                     call      0x1419d0
007D2969  488d97f8010000                 lea       rdx, [rdi + 0x1f8]
007D2970  41b901000000                   mov       r9d, 1
007D2976  4c8d0523c13001                 lea       r8, [rip + 0x130c123] ; RIP_RVA=0x1adeaa0
007D297D  488bcb                         mov       rcx, rbx
007D2980  e84bf096ff                     call      0x1419d0
007D2985  0fb68733010000                 movzx     eax, byte ptr [rdi + 0x133]
007D298C  4c8d4d28                       lea       r9, [rbp + 0x28]
007D2990  4c8b05797e3601                 mov       r8, qword ptr [rip + 0x1367e79] ; RIP_RVA=0x1b3a810
007D2997  488d15d2303701                 lea       rdx, [rip + 0x13730d2] ; RIP_RVA=0x1b45a70
007D299E  2401                           and       al, 1
007D29A0  c644242000                     mov       byte ptr [rsp + 0x20], 0
007D29A5  488bcb                         mov       rcx, rbx
007D29A8  884520                         mov       byte ptr [rbp + 0x20], al
007D29AB  e820fb3f00                     call      0xbd24d0
007D29B0  85c0                           test      eax, eax
007D29B2  746f                           je        0x7d2a23
007D29B4  83f801                         cmp       eax, 1
007D29B7  7c50                           jl        0x7d2a09
007D29B9  488b83d0000000                 mov       rax, qword ptr [rbx + 0xd0]
007D29C0  48635368                       movsxd    rdx, dword ptr [rbx + 0x68]
007D29C4  480faf5370                     imul      rdx, qword ptr [rbx + 0x70]
007D29C9  488b4020                       mov       rax, qword ptr [rax + 0x20]
007D29CD  482bc2                         sub       rax, rdx
007D29D0  48034350                       add       rax, qword ptr [rbx + 0x50]
007D29D4  48894348                       mov       qword ptr [rbx + 0x48], rax
007D29D8  483b4350                       cmp       rax, qword ptr [rbx + 0x50]
007D29DC  7216                           jb        0x7d29f4
007D29DE  488d4801                       lea       rcx, [rax + 1]
007D29E2  483b4b58                       cmp       rcx, qword ptr [rbx + 0x58]
007D29E6  770c                           ja        0x7d29f4
007D29E8  0fb600                         movzx     eax, byte ptr [rax]
007D29EB  884520                         mov       byte ptr [rbp + 0x20], al
007D29EE  48894b48                       mov       qword ptr [rbx + 0x48], rcx
007D29F2  eb27                           jmp       0x7d2a1b
007D29F4  41b801000000                   mov       r8d, 1
007D29FA  488d5520                       lea       rdx, [rbp + 0x20]
007D29FE  488d4b48                       lea       rcx, [rbx + 0x48]
007D2A02  e8595b1b00                     call      0x988560
007D2A07  eb12                           jmp       0x7d2a1b
007D2A09  488b4528                       mov       rax, qword ptr [rbp + 0x28]
007D2A0D  4885c0                         test      rax, rax
007D2A10  7409                           je        0x7d2a1b
007D2A12  488bd3                         mov       rdx, rbx
007D2A15  488d4d20                       lea       rcx, [rbp + 0x20]
007D2A19  ffd0                           call      rax
007D2A1B  488bcb                         mov       rcx, rbx
007D2A1E  e88d074000                     call      0xbd31b0
007D2A23  0fb64520                       movzx     eax, byte ptr [rbp + 0x20]
007D2A27  4c8d4d28                       lea       r9, [rbp + 0x28]
007D2A2B  c1e018                         shl       eax, 0x18
007D2A2E  488d1553303701                 lea       rdx, [rip + 0x1373053] ; RIP_RVA=0x1b45a88
007D2A35  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D2A3B  488bcb                         mov       rcx, rbx
007D2A3E  2500000001                     and       eax, 0x1000000
007D2A43  c644242000                     mov       byte ptr [rsp + 0x20], 0
007D2A48  338730010000                   xor       eax, dword ptr [rdi + 0x130]
007D2A4E  898730010000                   mov       dword ptr [rdi + 0x130], eax
007D2A54  4c8b05b57d3601                 mov       r8, qword ptr [rip + 0x1367db5] ; RIP_RVA=0x1b3a810
007D2A5B  c1e819                         shr       eax, 0x19
007D2A5E  2401                           and       al, 1
007D2A60  884520                         mov       byte ptr [rbp + 0x20], al
007D2A63  e868fa3f00                     call      0xbd24d0
007D2A68  85c0                           test      eax, eax
007D2A6A  746f                           je        0x7d2adb
007D2A6C  83f801                         cmp       eax, 1
007D2A6F  7c50                           jl        0x7d2ac1
007D2A71  488b83d0000000                 mov       rax, qword ptr [rbx + 0xd0]
007D2A78  48635368                       movsxd    rdx, dword ptr [rbx + 0x68]
007D2A7C  480faf5370                     imul      rdx, qword ptr [rbx + 0x70]
007D2A81  488b4020                       mov       rax, qword ptr [rax + 0x20]
007D2A85  482bc2                         sub       rax, rdx
007D2A88  48034350                       add       rax, qword ptr [rbx + 0x50]
007D2A8C  48894348                       mov       qword ptr [rbx + 0x48], rax
007D2A90  483b4350                       cmp       rax, qword ptr [rbx + 0x50]
007D2A94  7216                           jb        0x7d2aac
007D2A96  488d4801                       lea       rcx, [rax + 1]
007D2A9A  483b4b58                       cmp       rcx, qword ptr [rbx + 0x58]
007D2A9E  770c                           ja        0x7d2aac
007D2AA0  0fb600                         movzx     eax, byte ptr [rax]
007D2AA3  884520                         mov       byte ptr [rbp + 0x20], al
007D2AA6  48894b48                       mov       qword ptr [rbx + 0x48], rcx
007D2AAA  eb27                           jmp       0x7d2ad3
007D2AAC  41b801000000                   mov       r8d, 1
007D2AB2  488d5520                       lea       rdx, [rbp + 0x20]
007D2AB6  488d4b48                       lea       rcx, [rbx + 0x48]
007D2ABA  e8a15a1b00                     call      0x988560
007D2ABF  eb12                           jmp       0x7d2ad3
007D2AC1  488b4528                       mov       rax, qword ptr [rbp + 0x28]
007D2AC5  4885c0                         test      rax, rax
007D2AC8  7409                           je        0x7d2ad3
007D2ACA  488bd3                         mov       rdx, rbx
007D2ACD  488d4d20                       lea       rcx, [rbp + 0x20]
007D2AD1  ffd0                           call      rax
007D2AD3  488bcb                         mov       rcx, rbx
007D2AD6  e8d5064000                     call      0xbd31b0
007D2ADB  0fb64520                       movzx     eax, byte ptr [rbp + 0x20]
007D2ADF  488d9738010000                 lea       rdx, [rdi + 0x138]
007D2AE6  81a730010000fffffffd           and       dword ptr [rdi + 0x130], 0xfdffffff
007D2AF0  4c8d05a92f3701                 lea       r8, [rip + 0x1372fa9] ; RIP_RVA=0x1b45aa0
007D2AF7  83e001                         and       eax, 1
007D2AFA  4533c9                         xor       r9d, r9d
007D2AFD  c1e019                         shl       eax, 0x19
007D2B00  488bcb                         mov       rcx, rbx
007D2B03  098730010000                   or        dword ptr [rdi + 0x130], eax
007D2B09  e882d39aff                     call      0x17fe90
007D2B0E  488d976c010000                 lea       rdx, [rdi + 0x16c]
007D2B15  41b901000000                   mov       r9d, 1
007D2B1B  4c8d05962f3701                 lea       r8, [rip + 0x1372f96] ; RIP_RVA=0x1b45ab8
007D2B22  488bcb                         mov       rcx, rbx
007D2B25  488b5c2460                     mov       rbx, qword ptr [rsp + 0x60]
007D2B2A  4883c430                       add       rsp, 0x30
007D2B2E  5f                             pop       rdi
007D2B2F  5e                             pop       rsi
007D2B30  5d                             pop       rbp
007D2B31  e90aa493ff                     jmp       0x10cf40
