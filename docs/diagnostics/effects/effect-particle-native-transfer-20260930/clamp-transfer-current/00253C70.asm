00253C70  48895c2408                     mov       qword ptr [rsp + 8], rbx
00253C75  48896c2410                     mov       qword ptr [rsp + 0x10], rbp
00253C7A  4889742418                     mov       qword ptr [rsp + 0x18], rsi
00253C7F  57                             push      rdi
00253C80  4883ec30                       sub       rsp, 0x30
00253C84  488bfa                         mov       rdi, rdx
00253C87  488bd9                         mov       rbx, rcx
00253C8A  e87124feff                     call      0x236100
00253C8F  488d4b10                       lea       rcx, [rbx + 0x10]
00253C93  488bd7                         mov       rdx, rdi
00253C96  4c8d05ab5b8801                 lea       r8, [rip + 0x1885bab] ; RIP_RVA=0x1ad9848
00253C9D  e87e19feff                     call      0x235620
00253CA2  488d4b30                       lea       rcx, [rbx + 0x30]
00253CA6  488bd7                         mov       rdx, rdi
00253CA9  4c8d059c5b8801                 lea       r8, [rip + 0x1885b9c] ; RIP_RVA=0x1ad984c
00253CB0  e86b19feff                     call      0x235620
00253CB5  488d4b50                       lea       rcx, [rbx + 0x50]
00253CB9  488bd7                         mov       rdx, rdi
00253CBC  4c8d058d5b8801                 lea       r8, [rip + 0x1885b8d] ; RIP_RVA=0x1ad9850
00253CC3  e85819feff                     call      0x235620
00253CC8  488d4b70                       lea       rcx, [rbx + 0x70]
00253CCC  488bd7                         mov       rdx, rdi
00253CCF  4c8d050a658901                 lea       r8, [rip + 0x189650a] ; RIP_RVA=0x1aea1e0
00253CD6  e84519feff                     call      0x235620
00253CDB  4c8b055e6a8e01                 mov       r8, qword ptr [rip + 0x18e6a5e] ; RIP_RVA=0x1b3a740
00253CE2  4c8d8bb1000000                 lea       r9, [rbx + 0xb1]
00253CE9  33ed                           xor       ebp, ebp
00253CEB  488d15fe648901                 lea       rdx, [rip + 0x18964fe] ; RIP_RVA=0x1aea1f0
00253CF2  488bcf                         mov       rcx, rdi
00253CF5  896c2420                       mov       dword ptr [rsp + 0x20], ebp
00253CF9  e8a2e59700                     call      0xbd22a0
00253CFE  488b4758                       mov       rax, qword ptr [rdi + 0x58]
00253D02  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
00253D06  48c1e205                       shl       rdx, 5
00253D0A  488b08                         mov       rcx, qword ptr [rax]
00253D0D  c7440a0c01000000               mov       dword ptr [rdx + rcx + 0xc], 1
00253D15  488bcf                         mov       rcx, rdi
00253D18  e8b3f39700                     call      0xbd30d0
00253D1D  4c8b051c6a8e01                 mov       r8, qword ptr [rip + 0x18e6a1c] ; RIP_RVA=0x1b3a740
00253D24  4c8d8bb0000000                 lea       r9, [rbx + 0xb0]
00253D2B  488d1566458901                 lea       rdx, [rip + 0x1894566] ; RIP_RVA=0x1ae8298
00253D32  896c2420                       mov       dword ptr [rsp + 0x20], ebp
00253D36  488bcf                         mov       rcx, rdi
00253D39  e862e59700                     call      0xbd22a0
00253D3E  488b4758                       mov       rax, qword ptr [rdi + 0x58]
00253D42  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
00253D46  48c1e205                       shl       rdx, 5
00253D4A  488b08                         mov       rcx, qword ptr [rax]
00253D4D  c7440a0c01000000               mov       dword ptr [rdx + rcx + 0xc], 1
00253D55  488bcf                         mov       rcx, rdi
00253D58  e873f39700                     call      0xbd30d0
00253D5D  4c8b05dc698e01                 mov       r8, qword ptr [rip + 0x18e69dc] ; RIP_RVA=0x1b3a740
00253D64  4c8d8bb2000000                 lea       r9, [rbx + 0xb2]
00253D6B  488d158e648901                 lea       rdx, [rip + 0x189648e] ; RIP_RVA=0x1aea200
00253D72  896c2420                       mov       dword ptr [rsp + 0x20], ebp
00253D76  488bcf                         mov       rcx, rdi
00253D79  e822e59700                     call      0xbd22a0
00253D7E  488b4758                       mov       rax, qword ptr [rdi + 0x58]
00253D82  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
00253D86  48c1e205                       shl       rdx, 5
00253D8A  488b08                         mov       rcx, qword ptr [rax]
00253D8D  c7440a0c01000000               mov       dword ptr [rdx + rcx + 0xc], 1
00253D95  488bcf                         mov       rcx, rdi
00253D98  e833f39700                     call      0xbd30d0
00253D9D  4c8b059c698e01                 mov       r8, qword ptr [rip + 0x18e699c] ; RIP_RVA=0x1b3a740
00253DA4  4c8d8bb3000000                 lea       r9, [rbx + 0xb3]
00253DAB  488d156e648901                 lea       rdx, [rip + 0x189646e] ; RIP_RVA=0x1aea220
00253DB2  896c2420                       mov       dword ptr [rsp + 0x20], ebp
00253DB6  488bcf                         mov       rcx, rdi
00253DB9  e8e2e49700                     call      0xbd22a0
00253DBE  488b4758                       mov       rax, qword ptr [rdi + 0x58]
00253DC2  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
00253DC6  48c1e205                       shl       rdx, 5
00253DCA  488b08                         mov       rcx, qword ptr [rax]
00253DCD  c7440a0c01000000               mov       dword ptr [rdx + rcx + 0xc], 1
00253DD5  488bcf                         mov       rcx, rdi
00253DD8  e8f3f29700                     call      0xbd30d0
00253DDD  488bcf                         mov       rcx, rdi
00253DE0  e8fbe19700                     call      0xbd1fe0
00253DE5  4c8b057c698e01                 mov       r8, qword ptr [rip + 0x18e697c] ; RIP_RVA=0x1b3a768
00253DEC  4c8d8bb4000000                 lea       r9, [rbx + 0xb4]
00253DF3  488d1546648901                 lea       rdx, [rip + 0x1896446] ; RIP_RVA=0x1aea240
00253DFA  896c2420                       mov       dword ptr [rsp + 0x20], ebp
00253DFE  488bcf                         mov       rcx, rdi
00253E01  e89ae49700                     call      0xbd22a0
00253E06  488b5760                       mov       rdx, qword ptr [rdi + 0x60]
00253E0A  488b4758                       mov       rax, qword ptr [rdi + 0x58]
00253E0E  48c1e205                       shl       rdx, 5
00253E12  488b08                         mov       rcx, qword ptr [rax]
00253E15  c7440a0c04000000               mov       dword ptr [rdx + rcx + 0xc], 4
00253E1D  488bcf                         mov       rcx, rdi
00253E20  e8abf29700                     call      0xbd30d0
00253E25  f30f108bb4000000               movss     xmm1, dword ptr [rbx + 0xb4]
00253E2D  0f57c0                         xorps     xmm0, xmm0
00253E30  0f2fc1                         comiss    xmm0, xmm1
00253E33  770c                           ja        0x253e41
00253E35  f30f1005235a8801               movss     xmm0, dword ptr [rip + 0x1885a23] ; RIP_RVA=0x1ad9860
00253E3D  f30f5dc1                       minss     xmm0, xmm1
00253E41  f30f1183b4000000               movss     dword ptr [rbx + 0xb4], xmm0
00253E49  4c8d0548118901                 lea       r8, [rip + 0x1891148] ; RIP_RVA=0x1ae4f98
00253E50  4881c390000000                 add       rbx, 0x90
00253E57  896c2420                       mov       dword ptr [rsp + 0x20], ebp
00253E5B  4c8bcb                         mov       r9, rbx
00253E5E  488d15e3638901                 lea       rdx, [rip + 0x18963e3] ; RIP_RVA=0x1aea248
00253E65  488bcf                         mov       rcx, rdi
00253E68  e833e49700                     call      0xbd22a0
00253E6D  488bd7                         mov       rdx, rdi
00253E70  488bcb                         mov       rcx, rbx
00253E73  e85839fbff                     call      0x2077d0
00253E78  488bcf                         mov       rcx, rdi
00253E7B  e850f29700                     call      0xbd30d0
00253E80  488bcb                         mov       rcx, rbx
00253E83  488b5c2440                     mov       rbx, qword ptr [rsp + 0x40]
00253E88  488b6c2448                     mov       rbp, qword ptr [rsp + 0x48]
00253E8D  488b742450                     mov       rsi, qword ptr [rsp + 0x50]
00253E92  4883c430                       add       rsp, 0x30
00253E96  5f                             pop       rdi
00253E97  e974a20000                     jmp       0x25e110
