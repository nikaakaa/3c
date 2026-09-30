0025E110  4053                           push      rbx
0025E112  4883ec40                       sub       rsp, 0x40
0025E116  f30f10490c                     movss     xmm1, dword ptr [rcx + 0xc]
0025E11B  488bd9                         mov       rbx, rcx
0025E11E  0f29742430                     movaps    xmmword ptr [rsp + 0x30], xmm6
0025E123  f30f1035a9848801               movss     xmm6, dword ptr [rip + 0x18884a9] ; RIP_RVA=0x1ae65d4
0025E12B  0f297c2420                     movaps    xmmword ptr [rsp + 0x20], xmm7
0025E130  0f57ff                         xorps     xmm7, xmm7
0025E133  0f2ff9                         comiss    xmm7, xmm1
0025E136  7605                           jbe       0x25e13d
0025E138  0f57c0                         xorps     xmm0, xmm0
0025E13B  eb07                           jmp       0x25e144
0025E13D  0f28c6                         movaps    xmm0, xmm6
0025E140  f30f5dc1                       minss     xmm0, xmm1
0025E144  f30f11410c                     movss     dword ptr [rcx + 0xc], xmm0
0025E149  e85224fbff                     call      0x2105a0
0025E14E  f30f104308                     movss     xmm0, dword ptr [rbx + 8]
0025E153  0f2ff8                         comiss    xmm7, xmm0
0025E156  7615                           jbe       0x25e16d
0025E158  f30f117b08                     movss     dword ptr [rbx + 8], xmm7
0025E15D  0f28742430                     movaps    xmm6, xmmword ptr [rsp + 0x30]
0025E162  0f287c2420                     movaps    xmm7, xmmword ptr [rsp + 0x20]
0025E167  4883c440                       add       rsp, 0x40
0025E16B  5b                             pop       rbx
0025E16C  c3                             ret       
0025E16D  0f287c2420                     movaps    xmm7, xmmword ptr [rsp + 0x20]
0025E172  f30f5df0                       minss     xmm6, xmm0
0025E176  f30f117308                     movss     dword ptr [rbx + 8], xmm6
0025E17B  0f28742430                     movaps    xmm6, xmmword ptr [rsp + 0x30]
0025E180  4883c440                       add       rsp, 0x40
0025E184  5b                             pop       rbx
0025E185  c3                             ret       
