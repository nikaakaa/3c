174DDC50  56                             push      rsi
174DDC51  57                             push      rdi
174DDC52  53                             push      rbx
174DDC53  4883ec30                       sub       rsp, 0x30
174DDC57  0f29742420                     movaps    xmmword ptr [rsp + 0x20], xmm6
174DDC5C  0f28f1                         movaps    xmm6, xmm1
174DDC5F  4889ce                         mov       rsi, rcx
174DDC62  803d67763aee00                 cmp       byte ptr [rip - 0x11c58999], 0 ; RIP_RVA=0x58852d0
174DDC69  0f84fa010000                   je        0x174dde69
174DDC6F  803dafaaeeed00                 cmp       byte ptr [rip - 0x12115551], 0 ; RIP_RVA=0x53c8725
174DDC76  0f850b020000                   jne       0x174dde87
174DDC7C  488b4620                       mov       rax, qword ptr [rsi + 0x20]
174DDC80  4885c0                         test      rax, rax
174DDC83  0f849e040000                   je        0x174de127
174DDC89  4883b80804000000               cmp       qword ptr [rax + 0x408], 0
174DDC91  0f8490040000                   je        0x174de127
174DDC97  488b054a600dee                 mov       rax, qword ptr [rip - 0x11f29fb6] ; RIP_RVA=0x55b3ce8
174DDC9E  488b38                         mov       rdi, qword ptr [rax]
174DDCA1  f687cc00000001                 test      byte ptr [rdi + 0xcc], 1
174DDCA8  0f8406020000                   je        0x174ddeb4
174DDCAE  488b4768                       mov       rax, qword ptr [rdi + 0x68]
174DDCB2  488b5810                       mov       rbx, qword ptr [rax + 0x10]
174DDCB6  4885db                         test      rbx, rbx
174DDCB9  0f840e020000                   je        0x174ddecd
174DDCBF  f683cc00000001                 test      byte ptr [rbx + 0xcc], 1
174DDCC6  0f841b020000                   je        0x174ddee7
174DDCCC  488b4360                       mov       rax, qword ptr [rbx + 0x60]
174DDCD0  48833800                       cmp       qword ptr [rax], 0
174DDCD4  0f84ed020000                   je        0x174ddfc7
174DDCDA  488b0507600dee                 mov       rax, qword ptr [rip - 0x11f29ff9] ; RIP_RVA=0x55b3ce8
174DDCE1  488b38                         mov       rdi, qword ptr [rax]
174DDCE4  f687cc00000001                 test      byte ptr [rdi + 0xcc], 1
174DDCEB  0f8411020000                   je        0x174ddf02
174DDCF1  488b4768                       mov       rax, qword ptr [rdi + 0x68]
174DDCF5  488b5810                       mov       rbx, qword ptr [rax + 0x10]
174DDCF9  4885db                         test      rbx, rbx
174DDCFC  0f8419020000                   je        0x174ddf1b
174DDD02  f683cc00000001                 test      byte ptr [rbx + 0xcc], 1
174DDD09  0f8426020000                   je        0x174ddf35
174DDD0F  488b4360                       mov       rax, qword ptr [rbx + 0x60]
174DDD13  488b00                         mov       rax, qword ptr [rax]
174DDD16  4885c0                         test      rax, rax
174DDD19  0f842e020000                   je        0x174ddf4d
174DDD1F  488bb8d0010000                 mov       rdi, qword ptr [rax + 0x1d0]
174DDD26  488b0d53a4f3ed                 mov       rcx, qword ptr [rip - 0x120c5bad] ; RIP_RVA=0x5418180
174DDD2D  80b9cb00000000                 cmp       byte ptr [rcx + 0xcb], 0
174DDD34  0f8418020000                   je        0x174ddf52
174DDD3A  803da18837ee00                 cmp       byte ptr [rip - 0x11c8775f], 0 ; RIP_RVA=0x58565e2
174DDD41  0f841d020000                   je        0x174ddf64
174DDD47  488b0d32a4f3ed                 mov       rcx, qword ptr [rip - 0x120c5bce] ; RIP_RVA=0x5418180
174DDD4E  80b9cb00000000                 cmp       byte ptr [rcx + 0xcb], 0
174DDD55  0f842e020000                   je        0x174ddf89
174DDD5B  803d6d8837ee00                 cmp       byte ptr [rip - 0x11c87793], 0 ; RIP_RVA=0x58565cf
174DDD62  0f8433020000                   je        0x174ddf9b
174DDD68  4885ff                         test      rdi, rdi
174DDD6B  0f8456020000                   je        0x174ddfc7
174DDD71  488b0d08a4f3ed                 mov       rcx, qword ptr [rip - 0x120c5bf8] ; RIP_RVA=0x5418180
174DDD78  80b9cb00000000                 cmp       byte ptr [rcx + 0xcb], 0
174DDD7F  0f8432020000                   je        0x174ddfb7
174DDD85  48837f1000                     cmp       qword ptr [rdi + 0x10], 0
174DDD8A  0f8437020000                   je        0x174ddfc7
174DDD90  488b05515f0dee                 mov       rax, qword ptr [rip - 0x11f2a0af] ; RIP_RVA=0x55b3ce8
174DDD97  488b38                         mov       rdi, qword ptr [rax]
174DDD9A  f687cc00000001                 test      byte ptr [rdi + 0xcc], 1
174DDDA1  0f8497030000                   je        0x174de13e
174DDDA7  488b4768                       mov       rax, qword ptr [rdi + 0x68]
174DDDAB  488b5810                       mov       rbx, qword ptr [rax + 0x10]
174DDDAF  4885db                         test      rbx, rbx
174DDDB2  0f849f030000                   je        0x174de157
174DDDB8  f683cc00000001                 test      byte ptr [rbx + 0xcc], 1
174DDDBF  0f84ac030000                   je        0x174de171
174DDDC5  488b4360                       mov       rax, qword ptr [rbx + 0x60]
174DDDC9  488b00                         mov       rax, qword ptr [rax]
174DDDCC  4885c0                         test      rax, rax
174DDDCF  0f84b4030000                   je        0x174de189
174DDDD5  488b88d0010000                 mov       rcx, qword ptr [rax + 0x1d0]
174DDDDC  4885c9                         test      rcx, rcx
174DDDDF  0f84a9030000                   je        0x174de18e
174DDDE5  e8764000fc                     call      0x134e1e60
174DDDEA  84c0                           test      al, al
174DDDEC  0f84d5010000                   je        0x174ddfc7
174DDDF2  488b4620                       mov       rax, qword ptr [rsi + 0x20]
174DDDF6  4885c0                         test      rax, rax
174DDDF9  0f8494030000                   je        0x174de193
174DDDFF  488b8008040000                 mov       rax, qword ptr [rax + 0x408]
174DDE06  4885c0                         test      rax, rax
174DDE09  0f8489030000                   je        0x174de198
174DDE0F  8b5030                         mov       edx, dword ptr [rax + 0x30]
174DDE12  83fa03                         cmp       edx, 3
174DDE15  7429                           je        0x174dde40
174DDE17  83fa02                         cmp       edx, 2
174DDE1A  0f8400030000                   je        0x174de120
174DDE20  85d2                           test      edx, edx
174DDE22  0f85e1010000                   jne       0x174de009
174DDE28  c740500000803f                 mov       dword ptr [rax + 0x50], 0x3f800000
174DDE2F  c7403001000000                 mov       dword ptr [rax + 0x30], 1
174DDE36  f30f10053e6122eb               movss     xmm0, dword ptr [rip - 0x14dd9ec2] ; RIP_RVA=0x2703f7c
174DDE3E  eb0c                           jmp       0x174dde4c
174DDE40  c7403001000000                 mov       dword ptr [rax + 0x30], 1
174DDE47  f30f104050                     movss     xmm0, dword ptr [rax + 0x50]
174DDE4C  488d4830                       lea       rcx, [rax + 0x30]
174DDE50  f30f114060                     movss     dword ptr [rax + 0x60], xmm0
174DDE55  c7403c8fc2f53d                 mov       dword ptr [rax + 0x3c], 0x3df5c28f
174DDE5C  f30f10059c4d36eb               movss     xmm0, dword ptr [rip - 0x14c9b264] ; RIP_RVA=0x2842c00
174DDE64  e9bb010000                     jmp       0x174de024
174DDE69  b9e0380300                     mov       ecx, 0x338e0
174DDE6E  e86db5d9e8                     call      0x2793e0
174DDE73  c60556743aee01                 mov       byte ptr [rip - 0x11c58baa], 1 ; RIP_RVA=0x58852d0
174DDE7A  803da4a8eeed00                 cmp       byte ptr [rip - 0x1211575c], 0 ; RIP_RVA=0x53c8725
174DDE81  0f84f5fdffff                   je        0x174ddc7c
174DDE87  b975b40200                     mov       ecx, 0x2b475
174DDE8C  e8cff8c9f8                     call      0x1017d760
174DDE91  4885c0                         test      rax, rax
174DDE94  0f8403030000                   je        0x174de19d
174DDE9A  4889c1                         mov       rcx, rax
174DDE9D  4889f2                         mov       rdx, rsi
174DDEA0  0f28d6                         movaps    xmm2, xmm6
174DDEA3  0f28742420                     movaps    xmm6, xmmword ptr [rsp + 0x20]
174DDEA8  4883c430                       add       rsp, 0x30
174DDEAC  5b                             pop       rbx
174DDEAD  5f                             pop       rdi
174DDEAE  5e                             pop       rsi
174DDEAF  e98cc6caf3                     jmp       0xb18a540
174DDEB4  4889f9                         mov       rcx, rdi
174DDEB7  e8e40ed9e8                     call      0x26eda0
174DDEBC  488b4768                       mov       rax, qword ptr [rdi + 0x68]
174DDEC0  488b5810                       mov       rbx, qword ptr [rax + 0x10]
174DDEC4  4885db                         test      rbx, rbx
174DDEC7  0f85f2fdffff                   jne       0x174ddcbf
174DDECD  4889f9                         mov       rcx, rdi
174DDED0  31d2                           xor       edx, edx
174DDED2  e87920d9e8                     call      0x26ff50
174DDED7  488b18                         mov       rbx, qword ptr [rax]
174DDEDA  f683cc00000001                 test      byte ptr [rbx + 0xcc], 1
174DDEE1  0f85e5fdffff                   jne       0x174ddccc
174DDEE7  4889d9                         mov       rcx, rbx
174DDEEA  e8b10ed9e8                     call      0x26eda0
174DDEEF  488b4360                       mov       rax, qword ptr [rbx + 0x60]
174DDEF3  48833800                       cmp       qword ptr [rax], 0
174DDEF7  0f85ddfdffff                   jne       0x174ddcda
174DDEFD  e9c5000000                     jmp       0x174ddfc7
174DDF02  4889f9                         mov       rcx, rdi
174DDF05  e8960ed9e8                     call      0x26eda0
174DDF0A  488b4768                       mov       rax, qword ptr [rdi + 0x68]
174DDF0E  488b5810                       mov       rbx, qword ptr [rax + 0x10]
174DDF12  4885db                         test      rbx, rbx
174DDF15  0f85e7fdffff                   jne       0x174ddd02
174DDF1B  4889f9                         mov       rcx, rdi
174DDF1E  31d2                           xor       edx, edx
174DDF20  e82b20d9e8                     call      0x26ff50
174DDF25  488b18                         mov       rbx, qword ptr [rax]
174DDF28  f683cc00000001                 test      byte ptr [rbx + 0xcc], 1
174DDF2F  0f85dafdffff                   jne       0x174ddd0f
174DDF35  4889d9                         mov       rcx, rbx
174DDF38  e8630ed9e8                     call      0x26eda0
174DDF3D  488b4360                       mov       rax, qword ptr [rbx + 0x60]
174DDF41  488b00                         mov       rax, qword ptr [rax]
174DDF44  4885c0                         test      rax, rax
174DDF47  0f85d2fdffff                   jne       0x174ddd1f
174DDF4D  e8ceb15ee9                     call      0xac9120
174DDF52  e8f93ed9e8                     call      0x271e50
174DDF57  803d848637ee00                 cmp       byte ptr [rip - 0x11c8797c], 0 ; RIP_RVA=0x58565e2
174DDF5E  0f85e3fdffff                   jne       0x174ddd47
174DDF64  b9f24b0000                     mov       ecx, 0x4bf2
174DDF69  e872b4d9e8                     call      0x2793e0
174DDF6E  c6056d8637ee01                 mov       byte ptr [rip - 0x11c87993], 1 ; RIP_RVA=0x58565e2
174DDF75  488b0d04a2f3ed                 mov       rcx, qword ptr [rip - 0x120c5dfc] ; RIP_RVA=0x5418180
174DDF7C  80b9cb00000000                 cmp       byte ptr [rcx + 0xcb], 0
174DDF83  0f85d2fdffff                   jne       0x174ddd5b
174DDF89  e8c23ed9e8                     call      0x271e50
174DDF8E  803d3a8637ee00                 cmp       byte ptr [rip - 0x11c879c6], 0 ; RIP_RVA=0x58565cf
174DDF95  0f85cdfdffff                   jne       0x174ddd68
174DDF9B  b9df4b0000                     mov       ecx, 0x4bdf
174DDFA0  e83bb4d9e8                     call      0x2793e0
174DDFA5  c605238637ee01                 mov       byte ptr [rip - 0x11c879dd], 1 ; RIP_RVA=0x58565cf
174DDFAC  4885ff                         test      rdi, rdi
174DDFAF  0f85bcfdffff                   jne       0x174ddd71
174DDFB5  eb10                           jmp       0x174ddfc7
174DDFB7  e8943ed9e8                     call      0x271e50
174DDFBC  48837f1000                     cmp       qword ptr [rdi + 0x10], 0
174DDFC1  0f85c9fdffff                   jne       0x174ddd90
174DDFC7  488b4620                       mov       rax, qword ptr [rsi + 0x20]
174DDFCB  4885c0                         test      rax, rax
174DDFCE  0f8460010000                   je        0x174de134
174DDFD4  488b8008040000                 mov       rax, qword ptr [rax + 0x408]
174DDFDB  4885c0                         test      rax, rax
174DDFDE  0f8455010000                   je        0x174de139
174DDFE4  8b5030                         mov       edx, dword ptr [rax + 0x30]
174DDFE7  83fa02                         cmp       edx, 2
174DDFEA  0f8484000000                   je        0x174de074
174DDFF0  83fa01                         cmp       edx, 1
174DDFF3  0f848e000000                   je        0x174de087
174DDFF9  85d2                           test      edx, edx
174DDFFB  750c                           jne       0x174de009
174DDFFD  c740500000803f                 mov       dword ptr [rax + 0x50], 0x3f800000
174DE004  e91e010000                     jmp       0x174de127
174DE009  488d4830                       lea       rcx, [rax + 0x30]
174DE00D  83fa03                         cmp       edx, 3
174DE010  0f84f6000000                   je        0x174de10c
174DE016  83fa01                         cmp       edx, 1
174DE019  0f8508010000                   jne       0x174de127
174DE01F  f30f10403c                     movss     xmm0, dword ptr [rax + 0x3c]
174DE024  f30f5cc6                       subss     xmm0, xmm6
174DE028  f30f11403c                     movss     dword ptr [rax + 0x3c], xmm0
174DE02D  0f57c9                         xorps     xmm1, xmm1
174DE030  0f2ec8                         ucomiss   xmm1, xmm0
174DE033  0f83da000000                   jae       0x174de113
174DE039  f30f5e05c34b36eb               divss     xmm0, dword ptr [rip - 0x14c9b43d] ; RIP_RVA=0x2842c04
174DE041  f30f1015335f22eb               movss     xmm2, dword ptr [rip - 0x14dda0cd] ; RIP_RVA=0x2703f7c
174DE049  f30f58c2                       addss     xmm0, xmm2
174DE04D  f30f105860                     movss     xmm3, dword ptr [rax + 0x60]
174DE052  f30f5dd0                       minss     xmm2, xmm0
174DE056  f30fc2c101                     cmpltss   xmm0, xmm1
174DE05B  0f55c2                         andnps    xmm0, xmm2
174DE05E  f30f5ccb                       subss     xmm1, xmm3
174DE062  f30f59c8                       mulss     xmm1, xmm0
174DE066  f30f58cb                       addss     xmm1, xmm3
174DE06A  f30f114850                     movss     dword ptr [rax + 0x50], xmm1
174DE06F  e9b3000000                     jmp       0x174de127
174DE074  c7405000000000                 mov       dword ptr [rax + 0x50], 0
174DE07B  c7403003000000                 mov       dword ptr [rax + 0x30], 3
174DE082  0f57c0                         xorps     xmm0, xmm0
174DE085  eb0c                           jmp       0x174de093
174DE087  c7403003000000                 mov       dword ptr [rax + 0x30], 3
174DE08E  f30f104050                     movss     xmm0, dword ptr [rax + 0x50]
174DE093  488d4830                       lea       rcx, [rax + 0x30]
174DE097  f30f114060                     movss     dword ptr [rax + 0x60], xmm0
174DE09C  c7404c8fc2f53d                 mov       dword ptr [rax + 0x4c], 0x3df5c28f
174DE0A3  f30f1005554b36eb               movss     xmm0, dword ptr [rip - 0x14c9b4ab] ; RIP_RVA=0x2842c00
174DE0AB  f30f5cc6                       subss     xmm0, xmm6
174DE0AF  f30f11404c                     movss     dword ptr [rax + 0x4c], xmm0
174DE0B4  0f57c9                         xorps     xmm1, xmm1
174DE0B7  0f2ec8                         ucomiss   xmm1, xmm0
174DE0BA  733b                           jae       0x174de0f7
174DE0BC  f30f5e05404b36eb               divss     xmm0, dword ptr [rip - 0x14c9b4c0] ; RIP_RVA=0x2842c04
174DE0C4  f30f1015b05e22eb               movss     xmm2, dword ptr [rip - 0x14dda150] ; RIP_RVA=0x2703f7c
174DE0CC  f30f58c2                       addss     xmm0, xmm2
174DE0D0  f30f105860                     movss     xmm3, dword ptr [rax + 0x60]
174DE0D5  0f28e2                         movaps    xmm4, xmm2
174DE0D8  f30f5de0                       minss     xmm4, xmm0
174DE0DC  f30fc2c101                     cmpltss   xmm0, xmm1
174DE0E1  0f55c4                         andnps    xmm0, xmm4
174DE0E4  f30f5cd3                       subss     xmm2, xmm3
174DE0E8  f30f59d0                       mulss     xmm2, xmm0
174DE0EC  f30f58d3                       addss     xmm2, xmm3
174DE0F0  f30f115050                     movss     dword ptr [rax + 0x50], xmm2
174DE0F5  eb30                           jmp       0x174de127
174DE0F7  c70100000000                   mov       dword ptr [rcx], 0
174DE0FD  f20f10055b8934eb               movsd     xmm0, qword ptr [rip - 0x14cb76a5] ; RIP_RVA=0x2826a60
174DE105  f20f11404c                     movsd     qword ptr [rax + 0x4c], xmm0
174DE10A  eb1b                           jmp       0x174de127
174DE10C  f30f10404c                     movss     xmm0, dword ptr [rax + 0x4c]
174DE111  eb98                           jmp       0x174de0ab
174DE113  c7403c000080bf                 mov       dword ptr [rax + 0x3c], 0xbf800000
174DE11A  c70102000000                   mov       dword ptr [rcx], 2
174DE120  c7405000000000                 mov       dword ptr [rax + 0x50], 0
174DE127  0f28742420                     movaps    xmm6, xmmword ptr [rsp + 0x20]
174DE12C  4883c430                       add       rsp, 0x30
174DE130  5b                             pop       rbx
174DE131  5f                             pop       rdi
174DE132  5e                             pop       rsi
174DE133  c3                             ret       
174DE134  e8e7af5ee9                     call      0xac9120
174DE139  e8e2af5ee9                     call      0xac9120
174DE13E  4889f9                         mov       rcx, rdi
174DE141  e85a0cd9e8                     call      0x26eda0
174DE146  488b4768                       mov       rax, qword ptr [rdi + 0x68]
174DE14A  488b5810                       mov       rbx, qword ptr [rax + 0x10]
174DE14E  4885db                         test      rbx, rbx
174DE151  0f8561fcffff                   jne       0x174dddb8
174DE157  4889f9                         mov       rcx, rdi
174DE15A  31d2                           xor       edx, edx
174DE15C  e8ef1dd9e8                     call      0x26ff50
174DE161  488b18                         mov       rbx, qword ptr [rax]
174DE164  f683cc00000001                 test      byte ptr [rbx + 0xcc], 1
174DE16B  0f8554fcffff                   jne       0x174dddc5
174DE171  4889d9                         mov       rcx, rbx
174DE174  e8270cd9e8                     call      0x26eda0
174DE179  488b4360                       mov       rax, qword ptr [rbx + 0x60]
174DE17D  488b00                         mov       rax, qword ptr [rax]
174DE180  4885c0                         test      rax, rax
174DE183  0f854cfcffff                   jne       0x174dddd5
174DE189  e892af5ee9                     call      0xac9120
174DE18E  e88daf5ee9                     call      0xac9120
174DE193  e888af5ee9                     call      0xac9120
174DE198  e883af5ee9                     call      0xac9120
174DE19D  e87eaf5ee9                     call      0xac9120
174DE1A2  cc                             int3      
