01243F50  48895c2410                     mov       qword ptr [rsp + 0x10], rbx
01243F55  55                             push      rbp
01243F56  56                             push      rsi
01243F57  57                             push      rdi
01243F58  4154                           push      r12
01243F5A  4155                           push      r13
01243F5C  4156                           push      r14
01243F5E  4157                           push      r15
01243F60  4883ec50                       sub       rsp, 0x50
01243F64  488bf2                         mov       rsi, rdx
01243F67  488bf9                         mov       rdi, rcx
01243F6A  488bce                         mov       rcx, rsi
01243F6D  ba0b000000                     mov       edx, 0xb
01243F72  e889f598ff                     call      0xbd3500
01243F77  440fb6af55020000               movzx     r13d, byte ptr [rdi + 0x255]
01243F7F  4c8db708020000                 lea       r14, [rdi + 0x208]
01243F86  450fb726                       movzx     r12d, word ptr [r14]
01243F8A  488bd6                         mov       rdx, rsi
01243F8D  488bcf                         mov       rcx, rdi
01243F90  e80be058ff                     call      0x7d1fa0
01243F95  4533c9                         xor       r9d, r9d
01243F98  4c8d0521aa8900                 lea       r8, [rip + 0x89aa21] ; RIP_RVA=0x1ade9c0
01243F9F  498bd6                         mov       rdx, r14
01243FA2  488bce                         mov       rcx, rsi
01243FA5  e8c6bff3fe                     call      0x17ff70
01243FAA  488d9712020000                 lea       rdx, [rdi + 0x212]
01243FB1  4533c9                         xor       r9d, r9d
01243FB4  4c8d05853b9800                 lea       r8, [rip + 0x983b85] ; RIP_RVA=0x1bc7b40
01243FBB  488bce                         mov       rcx, rsi
01243FBE  e8adbff3fe                     call      0x17ff70
01243FC3  488d9714020000                 lea       rdx, [rdi + 0x214]
01243FCA  4533c9                         xor       r9d, r9d
01243FCD  4c8d057c3b9800                 lea       r8, [rip + 0x983b7c] ; RIP_RVA=0x1bc7b50
01243FD4  488bce                         mov       rcx, rsi
01243FD7  e894bff3fe                     call      0x17ff70
01243FDC  ba08000000                     mov       edx, 8
01243FE1  488bce                         mov       rcx, rsi
01243FE4  e8e7f298ff                     call      0xbd32d0
01243FE9  b902000000                     mov       ecx, 2
01243FEE  84c0                           test      al, al
01243FF0  7409                           je        0x1243ffb
01243FF2  66898f16020000                 mov       word ptr [rdi + 0x216], cx
01243FF9  eb19                           jmp       0x1244014
01243FFB  4533c9                         xor       r9d, r9d
01243FFE  4c8d055b3b9800                 lea       r8, [rip + 0x983b5b] ; RIP_RVA=0x1bc7b60
01244005  488d9716020000                 lea       rdx, [rdi + 0x216]
0124400C  488bce                         mov       rcx, rsi
0124400F  e85cbff3fe                     call      0x17ff70
01244014  488d9718020000                 lea       rdx, [rdi + 0x218]
0124401B  4533c9                         xor       r9d, r9d
0124401E  4c8d054b3b9800                 lea       r8, [rip + 0x983b4b] ; RIP_RVA=0x1bc7b70
01244025  488bce                         mov       rcx, rsi
01244028  e8138fecfe                     call      0x10cf40
0124402D  488d971c020000                 lea       rdx, [rdi + 0x21c]
01244034  4533c9                         xor       r9d, r9d
01244037  4c8d054a3b9800                 lea       r8, [rip + 0x983b4a] ; RIP_RVA=0x1bc7b88
0124403E  488bce                         mov       rcx, rsi
01244041  e8fa8eecfe                     call      0x10cf40
01244046  488d9720020000                 lea       rdx, [rdi + 0x220]
0124404D  4533c9                         xor       r9d, r9d
01244050  4c8d05493b9800                 lea       r8, [rip + 0x983b49] ; RIP_RVA=0x1bc7ba0
01244057  488bce                         mov       rcx, rsi
0124405A  e8e18eecfe                     call      0x10cf40
0124405F  488d9724020000                 lea       rdx, [rdi + 0x224]
01244066  4533c9                         xor       r9d, r9d
01244069  4c8d05483b9800                 lea       r8, [rip + 0x983b48] ; RIP_RVA=0x1bc7bb8
01244070  488bce                         mov       rcx, rsi
01244073  e8c88eecfe                     call      0x10cf40
01244078  488d9728020000                 lea       rdx, [rdi + 0x228]
0124407F  4533c9                         xor       r9d, r9d
01244082  4c8d053f3b9800                 lea       r8, [rip + 0x983b3f] ; RIP_RVA=0x1bc7bc8
01244089  488bce                         mov       rcx, rsi
0124408C  e8af8eecfe                     call      0x10cf40
01244091  488d972c020000                 lea       rdx, [rdi + 0x22c]
01244098  4533c9                         xor       r9d, r9d
0124409B  4c8d05ce769000                 lea       r8, [rip + 0x9076ce] ; RIP_RVA=0x1b4b770
012440A2  488bce                         mov       rcx, rsi
012440A5  e8968eecfe                     call      0x10cf40
012440AA  488d9730020000                 lea       rdx, [rdi + 0x230]
012440B1  4533c9                         xor       r9d, r9d
012440B4  4c8d051d3b9800                 lea       r8, [rip + 0x983b1d] ; RIP_RVA=0x1bc7bd8
012440BB  488bce                         mov       rcx, rsi
012440BE  e87d8eecfe                     call      0x10cf40
012440C3  488d9734020000                 lea       rdx, [rdi + 0x234]
012440CA  4533c9                         xor       r9d, r9d
012440CD  4c8d051c3b9800                 lea       r8, [rip + 0x983b1c] ; RIP_RVA=0x1bc7bf0
012440D4  488bce                         mov       rcx, rsi
012440D7  e8648eecfe                     call      0x10cf40
012440DC  488d9738020000                 lea       rdx, [rdi + 0x238]
012440E3  41b900008000                   mov       r9d, 0x800000
012440E9  4c8d05103b9800                 lea       r8, [rip + 0x983b10] ; RIP_RVA=0x1bc7c00
012440F0  488bce                         mov       rcx, rsi
012440F3  e8a835ebfe                     call      0xf76a0
012440F8  488daf3c020000                 lea       rbp, [rdi + 0x23c]
012440FF  4533c9                         xor       r9d, r9d
01244102  488bd5                         mov       rdx, rbp
01244105  4c8d05a4339000                 lea       r8, [rip + 0x9033a4] ; RIP_RVA=0x1b474b0
0124410C  488bce                         mov       rcx, rsi
0124410F  e86c90ecfe                     call      0x10d180
01244114  488d9748020000                 lea       rdx, [rdi + 0x248]
0124411B  4533c9                         xor       r9d, r9d
0124411E  4c8d05ef3a9800                 lea       r8, [rip + 0x983aef] ; RIP_RVA=0x1bc7c14
01244125  488bce                         mov       rcx, rsi
01244128  e85390ecfe                     call      0x10d180
0124412D  488d9754020000                 lea       rdx, [rdi + 0x254]
01244134  41b900008000                   mov       r9d, 0x800000
0124413A  4c8d05df3a9800                 lea       r8, [rip + 0x983adf] ; RIP_RVA=0x1bc7c20
01244141  488bce                         mov       rcx, rsi
01244144  e82772effe                     call      0x13b370
01244149  41b900008000                   mov       r9d, 0x800000
0124414F  4c8d05ea3a9800                 lea       r8, [rip + 0x983aea] ; RIP_RVA=0x1bc7c40
01244156  488d9755020000                 lea       rdx, [rdi + 0x255]
0124415D  488bce                         mov       rcx, rsi
01244160  e80b72effe                     call      0x13b370
01244165  488d9756020000                 lea       rdx, [rdi + 0x256]
0124416C  41b900008000                   mov       r9d, 0x800000
01244172  4c8d05df3a9800                 lea       r8, [rip + 0x983adf] ; RIP_RVA=0x1bc7c58
01244179  488bce                         mov       rcx, rsi
0124417C  e8ef71effe                     call      0x13b370
01244181  488d9757020000                 lea       rdx, [rdi + 0x257]
01244188  41b900008000                   mov       r9d, 0x800000
0124418E  4c8d05db3a9800                 lea       r8, [rip + 0x983adb] ; RIP_RVA=0x1bc7c70
01244195  488bce                         mov       rcx, rsi
01244198  e8d371effe                     call      0x13b370
0124419D  488d970a020000                 lea       rdx, [rdi + 0x20a]
012441A4  4533c9                         xor       r9d, r9d
012441A7  4c8d05d23a9800                 lea       r8, [rip + 0x983ad2] ; RIP_RVA=0x1bc7c80
012441AE  488bce                         mov       rcx, rsi
012441B1  e8ba71effe                     call      0x13b370
012441B6  488d9710020000                 lea       rdx, [rdi + 0x210]
012441BD  4533c9                         xor       r9d, r9d
012441C0  4c8d05d13a9800                 lea       r8, [rip + 0x983ad1] ; RIP_RVA=0x1bc7c98
012441C7  488bce                         mov       rcx, rsi
012441CA  e8a171effe                     call      0x13b370
012441CF  488d970c020000                 lea       rdx, [rdi + 0x20c]
012441D6  4533c9                         xor       r9d, r9d
012441D9  4c8d05d03a9800                 lea       r8, [rip + 0x983ad0] ; RIP_RVA=0x1bc7cb0
012441E0  488bce                         mov       rcx, rsi
012441E3  e8588decfe                     call      0x10cf40
012441E8  488d9708030000                 lea       rdx, [rdi + 0x308]
012441EF  4533c9                         xor       r9d, r9d
012441F2  4c8d05c73a9800                 lea       r8, [rip + 0x983ac7] ; RIP_RVA=0x1bc7cc0
012441F9  488bce                         mov       rcx, rsi
012441FC  e89f34ebfe                     call      0xf76a0
01244201  4c8b0518668f00                 mov       r8, qword ptr [rip + 0x8f6618] ; RIP_RVA=0x1b3a820
01244208  488d8f64020000                 lea       rcx, [rdi + 0x264]
0124420F  48894c2430                     mov       qword ptr [rsp + 0x30], rcx
01244214  4c8d8c2490000000               lea       r9, [rsp + 0x90]
0124421C  488bc7                         mov       rax, rdi
0124421F  c74424384c000000               mov       dword ptr [rsp + 0x38], 0x4c
01244227  482bc1                         sub       rax, rcx
0124422A  c644242001                     mov       byte ptr [rsp + 0x20], 1
0124422F  480592020000                   add       rax, 0x292
01244235  488d15a43a9800                 lea       rdx, [rip + 0x983aa4] ; RIP_RVA=0x1bc7ce0
0124423C  4889442440                     mov       qword ptr [rsp + 0x40], rax
01244241  488bce                         mov       rcx, rsi
01244244  4803c0                         add       rax, rax
01244247  4883c801                       or        rax, 1
0124424B  4889442448                     mov       qword ptr [rsp + 0x48], rax
01244250  e87be298ff                     call      0xbd24d0
01244255  85c0                           test      eax, eax
01244257  7436                           je        0x124428f
01244259  83f801                         cmp       eax, 1
0124425C  7c12                           jl        0x1244270
0124425E  4533c0                         xor       r8d, r8d
01244261  488d542430                     lea       rdx, [rsp + 0x30]
01244266  488bce                         mov       rcx, rsi
01244269  e832e911ff                     call      0x362ba0
0124426E  eb17                           jmp       0x1244287
01244270  488b842490000000               mov       rax, qword ptr [rsp + 0x90]
01244278  4885c0                         test      rax, rax
0124427B  740a                           je        0x1244287
0124427D  488bd6                         mov       rdx, rsi
01244280  488d4c2430                     lea       rcx, [rsp + 0x30]
01244285  ffd0                           call      rax
01244287  488bce                         mov       rcx, rsi
0124428A  e821ef98ff                     call      0xbd31b0
0124428F  488b442440                     mov       rax, qword ptr [rsp + 0x40]
01244294  488d9708040000                 lea       rdx, [rdi + 0x408]
0124429B  33db                           xor       ebx, ebx
0124429D  4c8d0530a88a00                 lea       r8, [rip + 0x8aa830] ; RIP_RVA=0x1aeead4
012442A4  4533c9                         xor       r9d, r9d
012442A7  488bce                         mov       rcx, rsi
012442AA  c68438640200002e               mov       byte ptr [rax + rdi + 0x264], 0x2e
012442B2  899f5c020000                   mov       dword ptr [rdi + 0x25c], ebx
012442B8  e8b37711ff                     call      0x35ba70
012442BD  488d970c040000                 lea       rdx, [rdi + 0x40c]
012442C4  4533c9                         xor       r9d, r9d
012442C7  4c8d05223a9800                 lea       r8, [rip + 0x983a22] ; RIP_RVA=0x1bc7cf0
012442CE  488bce                         mov       rcx, rsi
012442D1  e89a7711ff                     call      0x35ba70
012442D6  488d9710040000                 lea       rdx, [rdi + 0x410]
012442DD  4533c9                         xor       r9d, r9d
012442E0  4c8d05113a9800                 lea       r8, [rip + 0x983a11] ; RIP_RVA=0x1bc7cf8
012442E7  488bce                         mov       rcx, rsi
012442EA  e8817711ff                     call      0x35ba70
012442EF  488d9714040000                 lea       rdx, [rdi + 0x414]
012442F6  4533c9                         xor       r9d, r9d
012442F9  4c8d05003a9800                 lea       r8, [rip + 0x983a00] ; RIP_RVA=0x1bc7d00
01244300  488bce                         mov       rcx, rsi
01244303  e8687711ff                     call      0x35ba70
01244308  488d971c040000                 lea       rdx, [rdi + 0x41c]
0124430F  4533c9                         xor       r9d, r9d
01244312  4c8d05ef399800                 lea       r8, [rip + 0x9839ef] ; RIP_RVA=0x1bc7d08
01244319  488bce                         mov       rcx, rsi
0124431C  e84f7711ff                     call      0x35ba70
01244321  8d5309                         lea       edx, [rbx + 9]
01244324  488bce                         mov       rcx, rsi
01244327  e8a4ef98ff                     call      0xbd32d0
0124432C  84c0                           test      al, al
0124432E  7519                           jne       0x1244349
01244330  488d9720040000                 lea       rdx, [rdi + 0x420]
01244337  4533c9                         xor       r9d, r9d
0124433A  4c8d050f979000                 lea       r8, [rip + 0x90970f] ; RIP_RVA=0x1b4da50
01244341  488bce                         mov       rcx, rsi
01244344  e8277711ff                     call      0x35ba70
01244349  ba06000000                     mov       edx, 6
0124434E  488bce                         mov       rcx, rsi
01244351  e87aef98ff                     call      0xbd32d0
01244356  84c0                           test      al, al
01244358  7410                           je        0x124436a
0124435A  6641833e05                     cmp       word ptr [r14], 5
0124435F  7509                           jne       0x124436a
01244361  b806000000                     mov       eax, 6
01244366  66418906                       mov       word ptr [r14], ax
0124436A  ba05000000                     mov       edx, 5
0124436F  488bce                         mov       rcx, rsi
01244372  e859ef98ff                     call      0xbd32d0
01244377  84c0                           test      al, al
01244379  7406                           je        0x1244381
0124437B  889f56020000                   mov       byte ptr [rdi + 0x256], bl
01244381  ba04000000                     mov       edx, 4
01244386  488bce                         mov       rcx, rsi
01244389  e842ef98ff                     call      0xbd32d0
0124438E  84c0                           test      al, al
01244390  7406                           je        0x1244398
01244392  889f55020000                   mov       byte ptr [rdi + 0x255], bl
01244398  66453b26                       cmp       r12w, word ptr [r14]
0124439C  7509                           jne       0x12443a7
0124439E  443aaf55020000                 cmp       r13b, byte ptr [rdi + 0x255]
012443A5  7406                           je        0x12443ad
012443A7  899f5c020000                   mov       dword ptr [rdi + 0x25c], ebx
012443AD  ba03000000                     mov       edx, 3
012443B2  488bce                         mov       rcx, rsi
012443B5  e816ef98ff                     call      0xbd32d0
012443BA  84c0                           test      al, al
012443BC  7411                           je        0x12443cf
012443BE  6641833e04                     cmp       word ptr [r14], 4
012443C3  750a                           jne       0x12443cf
012443C5  c7873802000002000000           mov       dword ptr [rdi + 0x238], 2
012443CF  ba03000000                     mov       edx, 3
012443D4  488bce                         mov       rcx, rsi
012443D7  e8f4ee98ff                     call      0xbd32d0
012443DC  84c0                           test      al, al
012443DE  7439                           je        0x1244419
012443E0  6641833e01                     cmp       word ptr [r14], 1
012443E5  7532                           jne       0x1244419
012443E7  f30f104500                     movss     xmm0, dword ptr [rbp]
012443EC  f30f101568548900               movss     xmm2, dword ptr [rip + 0x895468] ; RIP_RVA=0x1ad985c
012443F4  f30f104d08                     movss     xmm1, dword ptr [rbp + 8]
012443F9  f30f59c2                       mulss     xmm0, xmm2
012443FD  f30f59ca                       mulss     xmm1, xmm2
01244401  f30f114500                     movss     dword ptr [rbp], xmm0
01244406  f30f104504                     movss     xmm0, dword ptr [rbp + 4]
0124440B  f30f59c2                       mulss     xmm0, xmm2
0124440F  f30f114d08                     movss     dword ptr [rbp + 8], xmm1
01244414  f30f114504                     movss     dword ptr [rbp + 4], xmm0
01244419  ba02000000                     mov       edx, 2
0124441E  488bce                         mov       rcx, rsi
01244421  e8aaee98ff                     call      0xbd32d0
01244426  84c0                           test      al, al
01244428  0f84c4010000                   je        0x12445f2
0124442E  4533c9                         xor       r9d, r9d
01244431  c78424900000001b000000         mov       dword ptr [rsp + 0x90], 0x1b
0124443C  4c8d05d5389800                 lea       r8, [rip + 0x9838d5] ; RIP_RVA=0x1bc7d18
01244443  488bce                         mov       rcx, rsi
01244446  488d942490000000               lea       rdx, [rsp + 0x90]
0124444E  e84d32ebfe                     call      0xf76a0
01244453  448b842490000000               mov       r8d, dword ptr [rsp + 0x90]
0124445B  41f6c001                       test      r8b, 1
0124445F  740b                           je        0x124446c
01244461  889f64020000                   mov       byte ptr [rdi + 0x264], bl
01244467  bb01000000                     mov       ebx, 1
0124446C  41f6c002                       test      r8b, 2
01244470  740c                           je        0x124447e
01244472  8bc3                           mov       eax, ebx
01244474  ffc3                           inc       ebx
01244476  c684386402000001               mov       byte ptr [rax + rdi + 0x264], 1
0124447E  41f6c004                       test      r8b, 4
01244482  740c                           je        0x1244490
01244484  8bc3                           mov       eax, ebx
01244486  ffc3                           inc       ebx
01244488  c684386402000002               mov       byte ptr [rax + rdi + 0x264], 2
01244490  41f6c008                       test      r8b, 8
01244494  740c                           je        0x12444a2
01244496  8bc3                           mov       eax, ebx
01244498  ffc3                           inc       ebx
0124449A  c684386402000003               mov       byte ptr [rax + rdi + 0x264], 3
012444A2  41f6c010                       test      r8b, 0x10
012444A6  7418                           je        0x12444c0
012444A8  8bc3                           mov       eax, ebx
012444AA  8d4b01                         lea       ecx, [rbx + 1]
012444AD  8d5901                         lea       ebx, [rcx + 1]
012444B0  c684386402000004               mov       byte ptr [rax + rdi + 0x264], 4
012444B8  c684396402000005               mov       byte ptr [rcx + rdi + 0x264], 5
012444C0  41f6c020                       test      r8b, 0x20
012444C4  7423                           je        0x12444e9
012444C6  8d4b01                         lea       ecx, [rbx + 1]
012444C9  8bc3                           mov       eax, ebx
012444CB  c684386402000005               mov       byte ptr [rax + rdi + 0x264], 5
012444D3  c684396402000008               mov       byte ptr [rcx + rdi + 0x264], 8
012444DB  8d4901                         lea       ecx, [rcx + 1]
012444DE  c684396402000009               mov       byte ptr [rcx + rdi + 0x264], 9
012444E6  8d5901                         lea       ebx, [rcx + 1]
012444E9  41f6c040                       test      r8b, 0x40
012444ED  7418                           je        0x1244507
012444EF  8bc3                           mov       eax, ebx
012444F1  8d4b01                         lea       ecx, [rbx + 1]
012444F4  8d5901                         lea       ebx, [rcx + 1]
012444F7  c68438640200000a               mov       byte ptr [rax + rdi + 0x264], 0xa
012444FF  c68439640200000b               mov       byte ptr [rcx + rdi + 0x264], 0xb
01244507  4584c0                         test      r8b, r8b
0124450A  7918                           jns       0x1244524
0124450C  8bc3                           mov       eax, ebx
0124450E  8d4b01                         lea       ecx, [rbx + 1]
01244511  8d5901                         lea       ebx, [rcx + 1]
01244514  c68438640200000e               mov       byte ptr [rax + rdi + 0x264], 0xe
0124451C  c68439640200000c               mov       byte ptr [rcx + rdi + 0x264], 0xc
01244524  410fbae008                     bt        r8d, 8
01244529  7318                           jae       0x1244543
0124452B  8bc3                           mov       eax, ebx
0124452D  8d4b01                         lea       ecx, [rbx + 1]
01244530  8d5901                         lea       ebx, [rcx + 1]
01244533  c684386402000010               mov       byte ptr [rax + rdi + 0x264], 0x10
0124453B  c68439640200000f               mov       byte ptr [rcx + rdi + 0x264], 0xf
01244543  410fbae009                     bt        r8d, 9
01244548  7318                           jae       0x1244562
0124454A  8bc3                           mov       eax, ebx
0124454C  8d4b01                         lea       ecx, [rbx + 1]
0124454F  8d5901                         lea       ebx, [rcx + 1]
01244552  c684386402000013               mov       byte ptr [rax + rdi + 0x264], 0x13
0124455A  c68439640200001f               mov       byte ptr [rcx + rdi + 0x264], 0x1f
01244562  410fbae00a                     bt        r8d, 0xa
01244567  7318                           jae       0x1244581
01244569  8bc3                           mov       eax, ebx
0124456B  8d4b01                         lea       ecx, [rbx + 1]
0124456E  8d5901                         lea       ebx, [rcx + 1]
01244571  c684386402000015               mov       byte ptr [rax + rdi + 0x264], 0x15
01244579  c684396402000016               mov       byte ptr [rcx + rdi + 0x264], 0x16
01244581  410fbae00b                     bt        r8d, 0xb
01244586  730c                           jae       0x1244594
01244588  8bc3                           mov       eax, ebx
0124458A  ffc3                           inc       ebx
0124458C  c684386402000022               mov       byte ptr [rax + rdi + 0x264], 0x22
01244594  410fbae00c                     bt        r8d, 0xc
01244599  730c                           jae       0x12445a7
0124459B  8bc3                           mov       eax, ebx
0124459D  ffc3                           inc       ebx
0124459F  c684386402000026               mov       byte ptr [rax + rdi + 0x264], 0x26
012445A7  410fbae00d                     bt        r8d, 0xd
012445AC  7318                           jae       0x12445c6
012445AE  8bc3                           mov       eax, ebx
012445B0  8d4b01                         lea       ecx, [rbx + 1]
012445B3  8d5901                         lea       ebx, [rcx + 1]
012445B6  c684386402000019               mov       byte ptr [rax + rdi + 0x264], 0x19
012445BE  c68439640200001b               mov       byte ptr [rcx + rdi + 0x264], 0x1b
012445C6  8bc3                           mov       eax, ebx
012445C8  ba01000000                     mov       edx, 1
012445CD  488bce                         mov       rcx, rsi
012445D0  c68438640200002e               mov       byte ptr [rax + rdi + 0x264], 0x2e
012445D8  e8f3ec98ff                     call      0xbd32d0
012445DD  84c0                           test      al, al
012445DF  7411                           je        0x12445f2
012445E1  83bc24900000001b               cmp       dword ptr [rsp + 0x90], 0x1b
012445E9  7407                           je        0x12445f2
012445EB  c6875402000001                 mov       byte ptr [rdi + 0x254], 1
012445F2  8b87c8070000                   mov       eax, dword ptr [rdi + 0x7c8]
012445F8  4c8d8c24a0000000               lea       r9, [rsp + 0xa0]
01244600  4c8b0569618f00                 mov       r8, qword ptr [rip + 0x8f6169] ; RIP_RVA=0x1b3a770
01244607  488d15aa808900                 lea       rdx, [rip + 0x8980aa] ; RIP_RVA=0x1adc6b8
0124460E  488bce                         mov       rcx, rsi
01244611  89842490000000                 mov       dword ptr [rsp + 0x90], eax
01244618  c644242000                     mov       byte ptr [rsp + 0x20], 0
0124461D  e8aede98ff                     call      0xbd24d0
01244622  85c0                           test      eax, eax
01244624  0f84bf000000                   je        0x12446e9
0124462A  83f801                         cmp       eax, 1
0124462D  0f8c94000000                   jl        0x12446c7
01244633  488b86d0000000                 mov       rax, qword ptr [rsi + 0xd0]
0124463A  48634e68                       movsxd    rcx, dword ptr [rsi + 0x68]
0124463E  480faf4e70                     imul      rcx, qword ptr [rsi + 0x70]
01244643  4c8b4020                       mov       r8, qword ptr [rax + 0x20]
01244647  4c2bc1                         sub       r8, rcx
0124464A  4c034650                       add       r8, qword ptr [rsi + 0x50]
0124464E  4c894648                       mov       qword ptr [rsi + 0x48], r8
01244652  4c3b4650                       cmp       r8, qword ptr [rsi + 0x50]
01244656  721a                           jb        0x1244672
01244658  498d4004                       lea       rax, [r8 + 4]
0124465C  483b4658                       cmp       rax, qword ptr [rsi + 0x58]
01244660  7710                           ja        0x1244672
01244662  418b10                         mov       edx, dword ptr [r8]
01244665  89942490000000                 mov       dword ptr [rsp + 0x90], edx
0124466C  48894648                       mov       qword ptr [rsi + 0x48], rax
01244670  eb1e                           jmp       0x1244690
01244672  41b804000000                   mov       r8d, 4
01244678  488d942490000000               lea       rdx, [rsp + 0x90]
01244680  488d4e48                       lea       rcx, [rsi + 0x48]
01244684  e8d73e74ff                     call      0x988560
01244689  8b942490000000                 mov       edx, dword ptr [rsp + 0x90]
01244690  8b06                           mov       eax, dword ptr [rsi]
01244692  48c1e809                       shr       rax, 9
01244696  a801                           test      al, 1
01244698  7447                           je        0x12446e1
0124469A  8bca                           mov       ecx, edx
0124469C  8bc2                           mov       eax, edx
0124469E  c1e810                         shr       eax, 0x10
012446A1  81e10000ff00                   and       ecx, 0xff0000
012446A7  0bc8                           or        ecx, eax
012446A9  8bc2                           mov       eax, edx
012446AB  c1e010                         shl       eax, 0x10
012446AE  81e200ff0000                   and       edx, 0xff00
012446B4  0bc2                           or        eax, edx
012446B6  c1e908                         shr       ecx, 8
012446B9  c1e008                         shl       eax, 8
012446BC  0bc8                           or        ecx, eax
012446BE  898c2490000000                 mov       dword ptr [rsp + 0x90], ecx
012446C5  eb1a                           jmp       0x12446e1
012446C7  488b8424a0000000               mov       rax, qword ptr [rsp + 0xa0]
012446CF  4885c0                         test      rax, rax
012446D2  740d                           je        0x12446e1
012446D4  488bd6                         mov       rdx, rsi
012446D7  488d8c2490000000               lea       rcx, [rsp + 0x90]
012446DF  ffd0                           call      rax
012446E1  488bce                         mov       rcx, rsi
012446E4  e8c7ea98ff                     call      0xbd31b0
012446E9  8b842490000000                 mov       eax, dword ptr [rsp + 0x90]
012446F0  488d97a8070000                 lea       rdx, [rdi + 0x7a8]
012446F7  4533c9                         xor       r9d, r9d
012446FA  8987c8070000                   mov       dword ptr [rdi + 0x7c8], eax
01244700  4c8d0529369800                 lea       r8, [rip + 0x983629] ; RIP_RVA=0x1bc7d30
01244707  488bce                         mov       rcx, rsi
0124470A  e8616ceffe                     call      0x13b370
0124470F  488d97ac070000                 lea       rdx, [rdi + 0x7ac]
01244716  4533c9                         xor       r9d, r9d
01244719  4c8d0528369800                 lea       r8, [rip + 0x983628] ; RIP_RVA=0x1bc7d48
01244720  488bce                         mov       rcx, rsi
01244723  e8588aecfe                     call      0x10d180
01244728  488d97b8070000                 lea       rdx, [rdi + 0x7b8]
0124472F  4533c9                         xor       r9d, r9d
01244732  4c8d0527369800                 lea       r8, [rip + 0x983627] ; RIP_RVA=0x1bc7d60
01244739  488bce                         mov       rcx, rsi
0124473C  e83f8aecfe                     call      0x10d180
01244741  488d4c2430                     lea       rcx, [rsp + 0x30]
01244746  e8d5f0eafe                     call      0xf3820
0124474B  488b9c2498000000               mov       rbx, qword ptr [rsp + 0x98]
01244753  4883c450                       add       rsp, 0x50
01244757  415f                           pop       r15
01244759  415e                           pop       r14
0124475B  415d                           pop       r13
0124475D  415c                           pop       r12
0124475F  5f                             pop       rdi
01244760  5e                             pop       rsi
01244761  5d                             pop       rbp
01244762  c3                             ret       
