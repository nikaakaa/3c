00236100  4053                           push      rbx
00236102  4883ec30                       sub       rsp, 0x30
00236106  4c8b0533469001                 mov       r8, qword ptr [rip + 0x1904633] ; RIP_RVA=0x1b3a740
0023610D  4c8d4908                       lea       r9, [rcx + 8]
00236111  488bda                         mov       rbx, rdx
00236114  c744242000000000               mov       dword ptr [rsp + 0x20], 0
0023611C  488bcb                         mov       rcx, rbx
0023611F  488d154a238b01                 lea       rdx, [rip + 0x18b234a] ; RIP_RVA=0x1ae8470
00236126  e875c19900                     call      0xbd22a0
0023612B  488b4358                       mov       rax, qword ptr [rbx + 0x58]
0023612F  4c8b4360                       mov       r8, qword ptr [rbx + 0x60]
00236133  49c1e005                       shl       r8, 5
00236137  488b08                         mov       rcx, qword ptr [rax]
0023613A  41c744080c01000000             mov       dword ptr [r8 + rcx + 0xc], 1
00236143  488bcb                         mov       rcx, rbx
00236146  e885cf9900                     call      0xbd30d0
0023614B  488bcb                         mov       rcx, rbx
0023614E  4883c430                       add       rsp, 0x30
00236152  5b                             pop       rbx
00236153  e988be9900                     jmp       0xbd1fe0
