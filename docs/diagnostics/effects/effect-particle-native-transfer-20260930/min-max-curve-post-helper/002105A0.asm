002105A0  4053                           push      rbx
002105A2  4883ec20                       sub       rsp, 0x20
002105A6  0fb74104                       movzx     eax, word ptr [rcx + 4]
002105AA  488bd9                         mov       rbx, rcx
002105AD  6685c0                         test      ax, ax
002105B0  0f8410010000                   je        0x2106c6
002105B6  6683f803                       cmp       ax, 3
002105BA  0f8406010000                   je        0x2106c6
002105C0  48897c2430                     mov       qword ptr [rsp + 0x30], rdi
002105C5  e886920000                     call      0x219850
002105CA  488b5318                       mov       rdx, qword ptr [rbx + 0x18]
002105CE  f30f10530c                     movss     xmm2, dword ptr [rbx + 0xc]
002105D3  488d8a40020000                 lea       rcx, [rdx + 0x240]
002105DA  e8215d6d00                     call      0x8e6300
002105DF  66837b0402                     cmp       word ptr [rbx + 4], 2
002105E4  0fb6f8                         movzx     edi, al
002105E7  754e                           jne       0x210637
002105E9  488bcb                         mov       rcx, rbx
002105EC  e88f970000                     call      0x219d80
002105F1  4084ff                         test      dil, dil
002105F4  7419                           je        0x21060f
002105F6  488b5310                       mov       rdx, qword ptr [rbx + 0x10]
002105FA  f30f10530c                     movss     xmm2, dword ptr [rbx + 0xc]
002105FF  488d8a40020000                 lea       rcx, [rdx + 0x240]
00210606  e8f55c6d00                     call      0x8e6300
0021060B  84c0                           test      al, al
0021060D  752e                           jne       0x21063d
0021060F  488b4b18                       mov       rcx, qword ptr [rbx + 0x18]
00210613  e848716d00                     call      0x8e7760
00210618  84c0                           test      al, al
0021061A  743a                           je        0x210656
0021061C  488b5318                       mov       rdx, qword ptr [rbx + 0x18]
00210620  f30f10530c                     movss     xmm2, dword ptr [rbx + 0xc]
00210625  488d4a70                       lea       rcx, [rdx + 0x70]
00210629  e8825a6d00                     call      0x8e60b0
0021062E  84c0                           test      al, al
00210630  7424                           je        0x210656
00210632  40b701                         mov       dil, 1
00210635  eb22                           jmp       0x210659
00210637  4080ff01                       cmp       dil, 1
0021063B  75d2                           jne       0x21060f
0021063D  66834b0601                     or        word ptr [rbx + 6], 1
00210642  bafdff0000                     mov       edx, 0xfffd
00210647  66215306                       and       word ptr [rbx + 6], dx
0021064B  488b7c2430                     mov       rdi, qword ptr [rsp + 0x30]
00210650  4883c420                       add       rsp, 0x20
00210654  5b                             pop       rbx
00210655  c3                             ret       
00210656  4032ff                         xor       dil, dil
00210659  66837b0402                     cmp       word ptr [rbx + 4], 2
0021065E  7538                           jne       0x210698
00210660  488bcb                         mov       rcx, rbx
00210663  e818970000                     call      0x219d80
00210668  4084ff                         test      dil, dil
0021066B  7428                           je        0x210695
0021066D  488b4b10                       mov       rcx, qword ptr [rbx + 0x10]
00210671  e8ea706d00                     call      0x8e7760
00210676  84c0                           test      al, al
00210678  741b                           je        0x210695
0021067A  488b5310                       mov       rdx, qword ptr [rbx + 0x10]
0021067E  f30f10530c                     movss     xmm2, dword ptr [rbx + 0xc]
00210683  488d4a70                       lea       rcx, [rdx + 0x70]
00210687  e8245a6d00                     call      0x8e60b0
0021068C  84c0                           test      al, al
0021068E  7405                           je        0x210695
00210690  40b701                         mov       dil, 1
00210693  eb03                           jmp       0x210698
00210695  4032ff                         xor       dil, dil
00210698  b8feff0000                     mov       eax, 0xfffe
0021069D  400fb6cf                       movzx     ecx, dil
002106A1  66214306                       and       word ptr [rbx + 6], ax
002106A5  bafdff0000                     mov       edx, 0xfffd
002106AA  0fb74306                       movzx     eax, word ptr [rbx + 6]
002106AE  6603c9                         add       cx, cx
002106B1  6623c2                         and       ax, dx
002106B4  660bc8                         or        cx, ax
002106B7  66894b06                       mov       word ptr [rbx + 6], cx
002106BB  488b7c2430                     mov       rdi, qword ptr [rsp + 0x30]
002106C0  4883c420                       add       rsp, 0x20
002106C4  5b                             pop       rbx
002106C5  c3                             ret       
002106C6  b8feff0000                     mov       eax, 0xfffe
002106CB  bafdff0000                     mov       edx, 0xfffd
002106D0  66214106                       and       word ptr [rcx + 6], ax
002106D4  66215106                       and       word ptr [rcx + 6], dx
002106D8  4883c420                       add       rsp, 0x20
002106DC  5b                             pop       rbx
002106DD  c3                             ret       
