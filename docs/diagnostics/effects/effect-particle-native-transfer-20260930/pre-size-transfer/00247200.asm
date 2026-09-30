00247200  4883ec28                       sub       rsp, 0x28
00247204  f30f104168                     movss     xmm0, dword ptr [rcx + 0x68]
00247209  0f57d2                         xorps     xmm2, xmm2
0024720C  0f2fd0                         comiss    xmm2, xmm0
0024720F  0f29742410                     movaps    xmmword ptr [rsp + 0x10], xmm6
00247214  0f28f1                         movaps    xmm6, xmm1
00247217  0f293c24                       movaps    xmmword ptr [rsp], xmm7
0024721B  7605                           jbe       0x247222
0024721D  0f57db                         xorps     xmm3, xmm3
00247220  eb07                           jmp       0x247229
00247222  0f28de                         movaps    xmm3, xmm6
00247225  f30f5dd8                       minss     xmm3, xmm0
00247229  0f57c0                         xorps     xmm0, xmm0
0024722C  f30f5fc3                       maxss     xmm0, xmm3
00247230  f30f101d104f8901               movss     xmm3, dword ptr [rip + 0x1894f10] ; RIP_RVA=0x1adc148
00247238  f30f114168                     movss     dword ptr [rcx + 0x68], xmm0
0024723D  f30f10a194000000               movss     xmm4, dword ptr [rcx + 0x94]
00247245  0f2fdc                         comiss    xmm3, xmm4
00247248  7603                           jbe       0x24724d
0024724A  0f28e3                         movaps    xmm4, xmm3
0024724D  f30f100d87788901               movss     xmm1, dword ptr [rip + 0x1897887] ; RIP_RVA=0x1adeadc
00247255  0f2fcc                         comiss    xmm1, xmm4
00247258  f30f103d34c58801               movss     xmm7, dword ptr [rip + 0x188c534] ; RIP_RVA=0x1ad3794
00247260  7605                           jbe       0x247267
00247262  0f28c1                         movaps    xmm0, xmm1
00247265  eb07                           jmp       0x24726e
00247267  0f28c7                         movaps    xmm0, xmm7
0024726A  f30f5dc4                       minss     xmm0, xmm4
0024726E  f30f118194000000               movss     dword ptr [rcx + 0x94], xmm0
00247276  f30f10a9a8000000               movss     xmm5, dword ptr [rcx + 0xa8]
0024727E  0f2fd5                         comiss    xmm2, xmm5
00247281  7605                           jbe       0x247288
00247283  0f57e4                         xorps     xmm4, xmm4
00247286  eb07                           jmp       0x24728f
00247288  0f28e6                         movaps    xmm4, xmm6
0024728B  f30f5de5                       minss     xmm4, xmm5
0024728F  0f57c0                         xorps     xmm0, xmm0
00247292  f30f5fc4                       maxss     xmm0, xmm4
00247296  f30f1181a8000000               movss     dword ptr [rcx + 0xa8], xmm0
0024729E  f30f10a1d4000000               movss     xmm4, dword ptr [rcx + 0xd4]
002472A6  0f2fdc                         comiss    xmm3, xmm4
002472A9  7603                           jbe       0x2472ae
002472AB  0f28e3                         movaps    xmm4, xmm3
002472AE  0f2fcc                         comiss    xmm1, xmm4
002472B1  7605                           jbe       0x2472b8
002472B3  0f28c1                         movaps    xmm0, xmm1
002472B6  eb07                           jmp       0x2472bf
002472B8  0f28c7                         movaps    xmm0, xmm7
002472BB  f30f5dc4                       minss     xmm0, xmm4
002472BF  f30f1181d4000000               movss     dword ptr [rcx + 0xd4], xmm0
002472C7  f30f10a9e8000000               movss     xmm5, dword ptr [rcx + 0xe8]
002472CF  0f2fd5                         comiss    xmm2, xmm5
002472D2  7605                           jbe       0x2472d9
002472D4  0f57e4                         xorps     xmm4, xmm4
002472D7  eb07                           jmp       0x2472e0
002472D9  0f28e6                         movaps    xmm4, xmm6
002472DC  f30f5de5                       minss     xmm4, xmm5
002472E0  0f57c0                         xorps     xmm0, xmm0
002472E3  f30f5fc4                       maxss     xmm0, xmm4
002472E7  f30f1181e8000000               movss     dword ptr [rcx + 0xe8], xmm0
002472EF  f30f10a114010000               movss     xmm4, dword ptr [rcx + 0x114]
002472F7  0f2fdc                         comiss    xmm3, xmm4
002472FA  7603                           jbe       0x2472ff
002472FC  0f28e3                         movaps    xmm4, xmm3
002472FF  0f2fcc                         comiss    xmm1, xmm4
00247302  7605                           jbe       0x247309
00247304  0f28c1                         movaps    xmm0, xmm1
00247307  eb07                           jmp       0x247310
00247309  0f28c7                         movaps    xmm0, xmm7
0024730C  f30f5dc4                       minss     xmm0, xmm4
00247310  f30f118114010000               movss     dword ptr [rcx + 0x114], xmm0
00247318  f30f10a928010000               movss     xmm5, dword ptr [rcx + 0x128]
00247320  0f2fd5                         comiss    xmm2, xmm5
00247323  7605                           jbe       0x24732a
00247325  0f57e4                         xorps     xmm4, xmm4
00247328  eb07                           jmp       0x247331
0024732A  0f28e6                         movaps    xmm4, xmm6
0024732D  f30f5de5                       minss     xmm4, xmm5
00247331  0f57c0                         xorps     xmm0, xmm0
00247334  f30f5fc4                       maxss     xmm0, xmm4
00247338  f30f118128010000               movss     dword ptr [rcx + 0x128], xmm0
00247340  f30f10a154010000               movss     xmm4, dword ptr [rcx + 0x154]
00247348  0f2fdc                         comiss    xmm3, xmm4
0024734B  7603                           jbe       0x247350
0024734D  0f28e3                         movaps    xmm4, xmm3
00247350  0f2fcc                         comiss    xmm1, xmm4
00247353  7605                           jbe       0x24735a
00247355  0f28c1                         movaps    xmm0, xmm1
00247358  eb07                           jmp       0x247361
0024735A  0f28c7                         movaps    xmm0, xmm7
0024735D  f30f5dc4                       minss     xmm0, xmm4
00247361  f30f118154010000               movss     dword ptr [rcx + 0x154], xmm0
00247369  f30f10a968010000               movss     xmm5, dword ptr [rcx + 0x168]
00247371  0f2fd5                         comiss    xmm2, xmm5
00247374  7605                           jbe       0x24737b
00247376  0f57e4                         xorps     xmm4, xmm4
00247379  eb07                           jmp       0x247382
0024737B  0f28e6                         movaps    xmm4, xmm6
0024737E  f30f5de5                       minss     xmm4, xmm5
00247382  0f57c0                         xorps     xmm0, xmm0
00247385  f30f5fc4                       maxss     xmm0, xmm4
00247389  f30f118168010000               movss     dword ptr [rcx + 0x168], xmm0
00247391  f30f10a194010000               movss     xmm4, dword ptr [rcx + 0x194]
00247399  0f2fdc                         comiss    xmm3, xmm4
0024739C  7603                           jbe       0x2473a1
0024739E  0f28e3                         movaps    xmm4, xmm3
002473A1  0f2fcc                         comiss    xmm1, xmm4
002473A4  7605                           jbe       0x2473ab
002473A6  0f28c1                         movaps    xmm0, xmm1
002473A9  eb07                           jmp       0x2473b2
002473AB  0f28c7                         movaps    xmm0, xmm7
002473AE  f30f5dc4                       minss     xmm0, xmm4
002473B2  f30f118194010000               movss     dword ptr [rcx + 0x194], xmm0
002473BA  f30f10a9a8010000               movss     xmm5, dword ptr [rcx + 0x1a8]
002473C2  0f2fd5                         comiss    xmm2, xmm5
002473C5  7605                           jbe       0x2473cc
002473C7  0f57e4                         xorps     xmm4, xmm4
002473CA  eb07                           jmp       0x2473d3
002473CC  0f28e6                         movaps    xmm4, xmm6
002473CF  f30f5de5                       minss     xmm4, xmm5
002473D3  0f57c0                         xorps     xmm0, xmm0
002473D6  f30f5fc4                       maxss     xmm0, xmm4
002473DA  f30f1181a8010000               movss     dword ptr [rcx + 0x1a8], xmm0
002473E2  f30f10a1d4010000               movss     xmm4, dword ptr [rcx + 0x1d4]
002473EA  0f2fdc                         comiss    xmm3, xmm4
002473ED  7603                           jbe       0x2473f2
002473EF  0f28e3                         movaps    xmm4, xmm3
002473F2  0f2fcc                         comiss    xmm1, xmm4
002473F5  7605                           jbe       0x2473fc
002473F7  0f28c1                         movaps    xmm0, xmm1
002473FA  eb07                           jmp       0x247403
002473FC  0f28c7                         movaps    xmm0, xmm7
002473FF  f30f5dc4                       minss     xmm0, xmm4
00247403  f30f1181d4010000               movss     dword ptr [rcx + 0x1d4], xmm0
0024740B  f30f10a9e8010000               movss     xmm5, dword ptr [rcx + 0x1e8]
00247413  0f2fd5                         comiss    xmm2, xmm5
00247416  7605                           jbe       0x24741d
00247418  0f57e4                         xorps     xmm4, xmm4
0024741B  eb07                           jmp       0x247424
0024741D  0f28e6                         movaps    xmm4, xmm6
00247420  f30f5de5                       minss     xmm4, xmm5
00247424  0f57c0                         xorps     xmm0, xmm0
00247427  f30f5fc4                       maxss     xmm0, xmm4
0024742B  f30f1181e8010000               movss     dword ptr [rcx + 0x1e8], xmm0
00247433  f30f10a114020000               movss     xmm4, dword ptr [rcx + 0x214]
0024743B  0f2fdc                         comiss    xmm3, xmm4
0024743E  7603                           jbe       0x247443
00247440  0f28e3                         movaps    xmm4, xmm3
00247443  0f2fcc                         comiss    xmm1, xmm4
00247446  7605                           jbe       0x24744d
00247448  0f28c1                         movaps    xmm0, xmm1
0024744B  eb07                           jmp       0x247454
0024744D  0f28c7                         movaps    xmm0, xmm7
00247450  f30f5dc4                       minss     xmm0, xmm4
00247454  f30f118114020000               movss     dword ptr [rcx + 0x214], xmm0
0024745C  f30f10a928020000               movss     xmm5, dword ptr [rcx + 0x228]
00247464  0f2fd5                         comiss    xmm2, xmm5
00247467  7605                           jbe       0x24746e
00247469  0f57e4                         xorps     xmm4, xmm4
0024746C  eb07                           jmp       0x247475
0024746E  0f28e6                         movaps    xmm4, xmm6
00247471  f30f5de5                       minss     xmm4, xmm5
00247475  0f57c0                         xorps     xmm0, xmm0
00247478  f30f5fc4                       maxss     xmm0, xmm4
0024747C  f30f118128020000               movss     dword ptr [rcx + 0x228], xmm0
00247484  f30f10a154020000               movss     xmm4, dword ptr [rcx + 0x254]
0024748C  0f2fdc                         comiss    xmm3, xmm4
0024748F  7703                           ja        0x247494
00247491  0f28dc                         movaps    xmm3, xmm4
00247494  0f2fcb                         comiss    xmm1, xmm3
00247497  7707                           ja        0x2474a0
00247499  0f28cf                         movaps    xmm1, xmm7
0024749C  f30f5dcb                       minss     xmm1, xmm3
002474A0  ba80000000                     mov       edx, 0x80
002474A5  f30f118954020000               movss     dword ptr [rcx + 0x254], xmm1
002474AD  8b8160020000                   mov       eax, dword ptr [rcx + 0x260]
002474B3  3bc2                           cmp       eax, edx
002474B5  0f4fc2                         cmovg     eax, edx
002474B8  4c63c0                         movsxd    r8, eax
002474BB  85c0                           test      eax, eax
002474BD  744b                           je        0x24750a
002474BF  33c0                           xor       eax, eax
002474C1  0f1f4000                       nop       dword ptr [rax]
002474C5  6666660f1f840000000000         nop       word ptr [rax + rax]
002474D0  488b9168020000                 mov       rdx, qword ptr [rcx + 0x268]
002474D7  f30f10441008                   movss     xmm0, dword ptr [rax + rdx + 8]
002474DD  0f2fd0                         comiss    xmm2, xmm0
002474E0  7605                           jbe       0x2474e7
002474E2  0f28ca                         movaps    xmm1, xmm2
002474E5  eb07                           jmp       0x2474ee
002474E7  0f28ce                         movaps    xmm1, xmm6
002474EA  f30f5dc8                       minss     xmm1, xmm0
002474EE  0f28c2                         movaps    xmm0, xmm2
002474F1  f30f5fc1                       maxss     xmm0, xmm1
002474F5  f30f11441008                   movss     dword ptr [rax + rdx + 8], xmm0
002474FB  836410141f                     and       dword ptr [rax + rdx + 0x14], 0x1f
00247500  4883c038                       add       rax, 0x38
00247504  4983e801                       sub       r8, 1
00247508  75c6                           jne       0x2474d0
0024750A  0f28742410                     movaps    xmm6, xmmword ptr [rsp + 0x10]
0024750F  0f283c24                       movaps    xmm7, xmmword ptr [rsp]
00247513  4883c428                       add       rsp, 0x28
00247517  c3                             ret       
