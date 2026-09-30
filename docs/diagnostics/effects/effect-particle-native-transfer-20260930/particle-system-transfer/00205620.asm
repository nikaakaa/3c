00205620  48895c2408                     mov       qword ptr [rsp + 8], rbx
00205625  48896c2410                     mov       qword ptr [rsp + 0x10], rbp
0020562A  4889742418                     mov       qword ptr [rsp + 0x18], rsi
0020562F  48897c2420                     mov       qword ptr [rsp + 0x20], rdi
00205634  4156                           push      r14
00205636  4883ec30                       sub       rsp, 0x30
0020563A  c6056782ca0100                 mov       byte ptr [rip + 0x1ca8267], 0 ; RIP_RVA=0x1ead8a8
00205641  488d1d5882ca01                 lea       rbx, [rip + 0x1ca8258] ; RIP_RVA=0x1ead8a0
00205648  488b4158                       mov       rax, qword ptr [rcx + 0x58]
0020564C  488bea                         mov       rbp, rdx
0020564F  488bf1                         mov       rsi, rcx
00205652  488d15ef008e01                 lea       rdx, [rip + 0x18e00ef] ; RIP_RVA=0x1ae5748
00205659  488bcd                         mov       rcx, rbp
0020565C  4c8b00                         mov       r8, qword ptr [rax]
0020565F  4d85c0                         test      r8, r8
00205662  490f45d8                       cmovne    rbx, r8
00205666  4533f6                         xor       r14d, r14d
00205669  4c8bcb                         mov       r9, rbx
0020566C  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205671  4c8d05d0008e01                 lea       r8, [rip + 0x18e00d0] ; RIP_RVA=0x1ae5748
00205678  e823cc9c00                     call      0xbd22a0
0020567D  488bd5                         mov       rdx, rbp
00205680  488bcb                         mov       rcx, rbx
00205683  e8a8690b00                     call      0x2bc030
00205688  488bcd                         mov       rcx, rbp
0020568B  e840da9c00                     call      0xbd30d0
00205690  4488351184ca01                 mov       byte ptr [rip + 0x1ca8411], r14b ; RIP_RVA=0x1eadaa8
00205697  488d3d0284ca01                 lea       rdi, [rip + 0x1ca8402] ; RIP_RVA=0x1eadaa0
0020569E  488b4658                       mov       rax, qword ptr [rsi + 0x58]
002056A2  4c8d0507018e01                 lea       r8, [rip + 0x18e0107] ; RIP_RVA=0x1ae57b0
002056A9  488d1500018e01                 lea       rdx, [rip + 0x18e0100] ; RIP_RVA=0x1ae57b0
002056B0  4489742420                     mov       dword ptr [rsp + 0x20], r14d
002056B5  488b4808                       mov       rcx, qword ptr [rax + 8]
002056B9  4885c9                         test      rcx, rcx
002056BC  480f45f9                       cmovne    rdi, rcx
002056C0  488bcd                         mov       rcx, rbp
002056C3  4c8bcf                         mov       r9, rdi
002056C6  e8d5cb9c00                     call      0xbd22a0
002056CB  488bd5                         mov       rdx, rbp
002056CE  488bcf                         mov       rcx, rdi
002056D1  e82af90501                     call      0x1265000
002056D6  488bcd                         mov       rcx, rbp
002056D9  e8f2d99c00                     call      0xbd30d0
002056DE  488bcf                         mov       rcx, rdi
002056E1  e8ba4c0601                     call      0x126a3a0
002056E6  4488355b86ca01                 mov       byte ptr [rip + 0x1ca865b], r14b ; RIP_RVA=0x1eadd48
002056ED  488d1d4c86ca01                 lea       rbx, [rip + 0x1ca864c] ; RIP_RVA=0x1eadd40
002056F4  488b4658                       mov       rax, qword ptr [rsi + 0x58]
002056F8  4c8d05c9ff8d01                 lea       r8, [rip + 0x18dffc9] ; RIP_RVA=0x1ae56c8
002056FF  488d15c2ff8d01                 lea       rdx, [rip + 0x18dffc2] ; RIP_RVA=0x1ae56c8
00205706  4489742420                     mov       dword ptr [rsp + 0x20], r14d
0020570B  488b4810                       mov       rcx, qword ptr [rax + 0x10]
0020570F  4885c9                         test      rcx, rcx
00205712  480f45d9                       cmovne    rbx, rcx
00205716  488bcd                         mov       rcx, rbp
00205719  4c8bcb                         mov       r9, rbx
0020571C  e87fcb9c00                     call      0xbd22a0
00205721  488bd5                         mov       rdx, rbp
00205724  488bcb                         mov       rcx, rbx
00205727  e834000300                     call      0x235760
0020572C  488bcd                         mov       rcx, rbp
0020572F  e89cd99c00                     call      0xbd30d0
00205734  488b4648                       mov       rax, qword ptr [rsi + 0x48]
00205738  488bcb                         mov       rcx, rbx
0020573B  f30f104838                     movss     xmm1, dword ptr [rax + 0x38]
00205740  e8bb1a0400                     call      0x247200
00205745  4488359c88ca01                 mov       byte ptr [rip + 0x1ca889c], r14b ; RIP_RVA=0x1eadfe8
0020574C  488d1d8d88ca01                 lea       rbx, [rip + 0x1ca888d] ; RIP_RVA=0x1eadfe0
00205753  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205757  4c8d0562008e01                 lea       r8, [rip + 0x18e0062] ; RIP_RVA=0x1ae57c0
0020575E  488bcd                         mov       rcx, rbp
00205761  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205766  488b5018                       mov       rdx, qword ptr [rax + 0x18]
0020576A  4885d2                         test      rdx, rdx
0020576D  480f45da                       cmovne    rbx, rdx
00205771  488d1548008e01                 lea       rdx, [rip + 0x18e0048] ; RIP_RVA=0x1ae57c0
00205778  4c8bcb                         mov       r9, rbx
0020577B  e820cb9c00                     call      0xbd22a0
00205780  488bd5                         mov       rdx, rbp
00205783  488bcb                         mov       rcx, rbx
00205786  e885990500                     call      0x25f110
0020578B  488bcd                         mov       rcx, rbp
0020578E  e83dd99c00                     call      0xbd30d0
00205793  448835ce88ca01                 mov       byte ptr [rip + 0x1ca88ce], r14b ; RIP_RVA=0x1eae068
0020579A  488d1dbf88ca01                 lea       rbx, [rip + 0x1ca88bf] ; RIP_RVA=0x1eae060
002057A1  488b4658                       mov       rax, qword ptr [rsi + 0x58]
002057A5  488b5020                       mov       rdx, qword ptr [rax + 0x20]
002057A9  4885d2                         test      rdx, rdx
002057AC  4489742420                     mov       dword ptr [rsp + 0x20], r14d
002057B1  4c8d05d0ff8d01                 lea       r8, [rip + 0x18dffd0] ; RIP_RVA=0x1ae5788
002057B8  488bcd                         mov       rcx, rbp
002057BB  480f45da                       cmovne    rbx, rdx
002057BF  488d15c2ff8d01                 lea       rdx, [rip + 0x18dffc2] ; RIP_RVA=0x1ae5788
002057C6  4c8bcb                         mov       r9, rbx
002057C9  e8d2ca9c00                     call      0xbd22a0
002057CE  488bd5                         mov       rdx, rbp
002057D1  488bcb                         mov       rcx, rbx
002057D4  e857ee0400                     call      0x254630
002057D9  488bcd                         mov       rcx, rbp
002057DC  e8efd89c00                     call      0xbd30d0
002057E1  448835701dc501                 mov       byte ptr [rip + 0x1c51d70], r14b ; RIP_RVA=0x1e57558
002057E8  488d1d611dc501                 lea       rbx, [rip + 0x1c51d61] ; RIP_RVA=0x1e57550
002057EF  488b4658                       mov       rax, qword ptr [rsi + 0x58]
002057F3  4c8d056efe8d01                 lea       r8, [rip + 0x18dfe6e] ; RIP_RVA=0x1ae5668
002057FA  488bcd                         mov       rcx, rbp
002057FD  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205802  488b5028                       mov       rdx, qword ptr [rax + 0x28]
00205806  4885d2                         test      rdx, rdx
00205809  480f45da                       cmovne    rbx, rdx
0020580D  488d1554fe8d01                 lea       rdx, [rip + 0x18dfe54] ; RIP_RVA=0x1ae5668
00205814  4c8bcb                         mov       r9, rbx
00205817  e884ca9c00                     call      0xbd22a0
0020581C  488bd5                         mov       rdx, rbp
0020581F  488bcb                         mov       rcx, rbx
00205822  e899fe0200                     call      0x2356c0
00205827  488bcd                         mov       rcx, rbp
0020582A  e8a1d89c00                     call      0xbd30d0
0020582F  448835b288ca01                 mov       byte ptr [rip + 0x1ca88b2], r14b ; RIP_RVA=0x1eae0e8
00205836  488d1da388ca01                 lea       rbx, [rip + 0x1ca88a3] ; RIP_RVA=0x1eae0e0
0020583D  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205841  4c8d0510008e01                 lea       r8, [rip + 0x18e0010] ; RIP_RVA=0x1ae5858
00205848  488bcd                         mov       rcx, rbp
0020584B  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205850  488b5030                       mov       rdx, qword ptr [rax + 0x30]
00205854  4885d2                         test      rdx, rdx
00205857  480f45da                       cmovne    rbx, rdx
0020585B  488d15f6ff8d01                 lea       rdx, [rip + 0x18dfff6] ; RIP_RVA=0x1ae5858
00205862  4c8bcb                         mov       r9, rbx
00205865  e836ca9c00                     call      0xbd22a0
0020586A  488bd5                         mov       rdx, rbp
0020586D  488bcb                         mov       rcx, rbx
00205870  e8bb9b0500                     call      0x25f430
00205875  488bcd                         mov       rcx, rbp
00205878  e853d89c00                     call      0xbd30d0
0020587D  488bcb                         mov       rcx, rbx
00205880  e8eb440a00                     call      0x2a9d70
00205885  4488353c89ca01                 mov       byte ptr [rip + 0x1ca893c], r14b ; RIP_RVA=0x1eae1c8
0020588C  488d1d2d89ca01                 lea       rbx, [rip + 0x1ca892d] ; RIP_RVA=0x1eae1c0
00205893  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205897  4c8d05daff8d01                 lea       r8, [rip + 0x18dffda] ; RIP_RVA=0x1ae5878
0020589E  488bcd                         mov       rcx, rbp
002058A1  4489742420                     mov       dword ptr [rsp + 0x20], r14d
002058A6  488b5038                       mov       rdx, qword ptr [rax + 0x38]
002058AA  4885d2                         test      rdx, rdx
002058AD  480f45da                       cmovne    rbx, rdx
002058B1  488d15c0ff8d01                 lea       rdx, [rip + 0x18dffc0] ; RIP_RVA=0x1ae5878
002058B8  4c8bcb                         mov       r9, rbx
002058BB  e8e0c99c00                     call      0xbd22a0
002058C0  488bd5                         mov       rdx, rbp
002058C3  488bcb                         mov       rcx, rbx
002058C6  e8752a0d00                     call      0x2d8340
002058CB  488bcd                         mov       rcx, rbp
002058CE  e8fdd79c00                     call      0xbd30d0
002058D3  448835ce1cc501                 mov       byte ptr [rip + 0x1c51cce], r14b ; RIP_RVA=0x1e575a8
002058DA  488d1dbf1cc501                 lea       rbx, [rip + 0x1c51cbf] ; RIP_RVA=0x1e575a0
002058E1  488b4658                       mov       rax, qword ptr [rsi + 0x58]
002058E5  4c8d0544fe8d01                 lea       r8, [rip + 0x18dfe44] ; RIP_RVA=0x1ae5730
002058EC  488bcd                         mov       rcx, rbp
002058EF  4489742420                     mov       dword ptr [rsp + 0x20], r14d
002058F4  488b5040                       mov       rdx, qword ptr [rax + 0x40]
002058F8  4885d2                         test      rdx, rdx
002058FB  480f45da                       cmovne    rbx, rdx
002058FF  488d152afe8d01                 lea       rdx, [rip + 0x18dfe2a] ; RIP_RVA=0x1ae5730
00205906  4c8bcb                         mov       r9, rbx
00205909  e892c99c00                     call      0xbd22a0
0020590E  488bd5                         mov       rdx, rbp
00205911  488bcb                         mov       rcx, rbx
00205914  e847660b00                     call      0x2bbf60
00205919  488bcd                         mov       rcx, rbp
0020591C  e8afd79c00                     call      0xbd30d0
00205921  448835208aca01                 mov       byte ptr [rip + 0x1ca8a20], r14b ; RIP_RVA=0x1eae348
00205928  488d1d118aca01                 lea       rbx, [rip + 0x1ca8a11] ; RIP_RVA=0x1eae340
0020592F  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205933  4c8d05d6fd8d01                 lea       r8, [rip + 0x18dfdd6] ; RIP_RVA=0x1ae5710
0020593A  488bcd                         mov       rcx, rbp
0020593D  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205942  488b5048                       mov       rdx, qword ptr [rax + 0x48]
00205946  4885d2                         test      rdx, rdx
00205949  480f45da                       cmovne    rbx, rdx
0020594D  488d15bcfd8d01                 lea       rdx, [rip + 0x18dfdbc] ; RIP_RVA=0x1ae5710
00205954  4c8bcb                         mov       r9, rbx
00205957  e844c99c00                     call      0xbd22a0
0020595C  488bd5                         mov       rdx, rbp
0020595F  488bcb                         mov       rcx, rbx
00205962  e8f9030300                     call      0x235d60
00205967  488bcd                         mov       rcx, rbp
0020596A  e861d79c00                     call      0xbd30d0
0020596F  448835721cc501                 mov       byte ptr [rip + 0x1c51c72], r14b ; RIP_RVA=0x1e575e8
00205976  488d1d631cc501                 lea       rbx, [rip + 0x1c51c63] ; RIP_RVA=0x1e575e0
0020597D  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205981  4c8d0560fd8d01                 lea       r8, [rip + 0x18dfd60] ; RIP_RVA=0x1ae56e8
00205988  488bcd                         mov       rcx, rbp
0020598B  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205990  488b5050                       mov       rdx, qword ptr [rax + 0x50]
00205994  4885d2                         test      rdx, rdx
00205997  480f45da                       cmovne    rbx, rdx
0020599B  488d1546fd8d01                 lea       rdx, [rip + 0x18dfd46] ; RIP_RVA=0x1ae56e8
002059A2  4c8bcb                         mov       r9, rbx
002059A5  e8f6c89c00                     call      0xbd22a0
002059AA  488bd5                         mov       rdx, rbp
002059AD  488bcb                         mov       rcx, rbx
002059B0  e83b250d00                     call      0x2d7ef0
002059B5  488bcd                         mov       rcx, rbp
002059B8  e813d79c00                     call      0xbd30d0
002059BD  448835448aca01                 mov       byte ptr [rip + 0x1ca8a44], r14b ; RIP_RVA=0x1eae408
002059C4  488d1d358aca01                 lea       rbx, [rip + 0x1ca8a35] ; RIP_RVA=0x1eae400
002059CB  488b4658                       mov       rax, qword ptr [rsi + 0x58]
002059CF  4c8d054afc8d01                 lea       r8, [rip + 0x18dfc4a] ; RIP_RVA=0x1ae5620
002059D6  488bcd                         mov       rcx, rbp
002059D9  4489742420                     mov       dword ptr [rsp + 0x20], r14d
002059DE  488b5058                       mov       rdx, qword ptr [rax + 0x58]
002059E2  4885d2                         test      rdx, rdx
002059E5  480f45da                       cmovne    rbx, rdx
002059E9  488d1530fc8d01                 lea       rdx, [rip + 0x18dfc30] ; RIP_RVA=0x1ae5620
002059F0  4c8bcb                         mov       r9, rbx
002059F3  e8a8c89c00                     call      0xbd22a0
002059F8  488bd5                         mov       rdx, rbp
002059FB  488bcb                         mov       rcx, rbx
002059FE  e86de20400                     call      0x253c70
00205A03  488bcd                         mov       rcx, rbp
00205A06  e8c5d69c00                     call      0xbd30d0
00205A0B  448835b68aca01                 mov       byte ptr [rip + 0x1ca8ab6], r14b ; RIP_RVA=0x1eae4c8
00205A12  488d1da78aca01                 lea       rbx, [rip + 0x1ca8aa7] ; RIP_RVA=0x1eae4c0
00205A19  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205A1D  4c8d0554fd8d01                 lea       r8, [rip + 0x18dfd54] ; RIP_RVA=0x1ae5778
00205A24  488bcd                         mov       rcx, rbp
00205A27  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205A2C  488b5060                       mov       rdx, qword ptr [rax + 0x60]
00205A30  4885d2                         test      rdx, rdx
00205A33  480f45da                       cmovne    rbx, rdx
00205A37  488d153afd8d01                 lea       rdx, [rip + 0x18dfd3a] ; RIP_RVA=0x1ae5778
00205A3E  4c8bcb                         mov       r9, rbx
00205A41  e85ac89c00                     call      0xbd22a0
00205A46  488bd5                         mov       rdx, rbp
00205A49  488bcb                         mov       rcx, rbx
00205A4C  e8ef690b00                     call      0x2bc440
00205A51  488bcd                         mov       rcx, rbp
00205A54  e877d69c00                     call      0xbd30d0
00205A59  448835e88bca01                 mov       byte ptr [rip + 0x1ca8be8], r14b ; RIP_RVA=0x1eae648
00205A60  488d1dd98bca01                 lea       rbx, [rip + 0x1ca8bd9] ; RIP_RVA=0x1eae640
00205A67  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205A6B  4c8d055efd8d01                 lea       r8, [rip + 0x18dfd5e] ; RIP_RVA=0x1ae57d0
00205A72  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205A77  488bcd                         mov       rcx, rbp
00205A7A  488b5068                       mov       rdx, qword ptr [rax + 0x68]
00205A7E  4885d2                         test      rdx, rdx
00205A81  480f45da                       cmovne    rbx, rdx
00205A85  488d1544fd8d01                 lea       rdx, [rip + 0x18dfd44] ; RIP_RVA=0x1ae57d0
00205A8C  4c8bcb                         mov       r9, rbx
00205A8F  e80cc89c00                     call      0xbd22a0
00205A94  488bd5                         mov       rdx, rbp
00205A97  488bcb                         mov       rcx, rbx
00205A9A  e8d10e0300                     call      0x236970
00205A9F  488bcd                         mov       rcx, rbp
00205AA2  e829d69c00                     call      0xbd30d0
00205AA7  4488351a8cca01                 mov       byte ptr [rip + 0x1ca8c1a], r14b ; RIP_RVA=0x1eae6c8
00205AAE  488d1d0b8cca01                 lea       rbx, [rip + 0x1ca8c0b] ; RIP_RVA=0x1eae6c0
00205AB5  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205AB9  4c8d05d8fc8d01                 lea       r8, [rip + 0x18dfcd8] ; RIP_RVA=0x1ae5798
00205AC0  488bcd                         mov       rcx, rbp
00205AC3  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205AC8  488b5070                       mov       rdx, qword ptr [rax + 0x70]
00205ACC  4885d2                         test      rdx, rdx
00205ACF  480f45da                       cmovne    rbx, rdx
00205AD3  488d15befc8d01                 lea       rdx, [rip + 0x18dfcbe] ; RIP_RVA=0x1ae5798
00205ADA  4c8bcb                         mov       r9, rbx
00205ADD  e8bec79c00                     call      0xbd22a0
00205AE2  488bd5                         mov       rdx, rbp
00205AE5  488bcb                         mov       rcx, rbx
00205AE8  e873a20200                     call      0x22fd60
00205AED  488bcd                         mov       rcx, rbp
00205AF0  e8dbd59c00                     call      0xbd30d0
00205AF5  4488354c1bc501                 mov       byte ptr [rip + 0x1c51b4c], r14b ; RIP_RVA=0x1e57648
00205AFC  488d1d3d1bc501                 lea       rbx, [rip + 0x1c51b3d] ; RIP_RVA=0x1e57640
00205B03  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205B07  4c8d057afb8d01                 lea       r8, [rip + 0x18dfb7a] ; RIP_RVA=0x1ae5688
00205B0E  488bcd                         mov       rcx, rbp
00205B11  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205B16  488b5078                       mov       rdx, qword ptr [rax + 0x78]
00205B1A  4885d2                         test      rdx, rdx
00205B1D  480f45da                       cmovne    rbx, rdx
00205B21  488d1560fb8d01                 lea       rdx, [rip + 0x18dfb60] ; RIP_RVA=0x1ae5688
00205B28  4c8bcb                         mov       r9, rbx
00205B2B  e870c79c00                     call      0xbd22a0
00205B30  488bd5                         mov       rdx, rbp
00205B33  488bcb                         mov       rcx, rbx
00205B36  e855220d00                     call      0x2d7d90
00205B3B  488bcd                         mov       rcx, rbp
00205B3E  e88dd59c00                     call      0xbd30d0
00205B43  448835fe8bca01                 mov       byte ptr [rip + 0x1ca8bfe], r14b ; RIP_RVA=0x1eae748
00205B4A  488d1def8bca01                 lea       rbx, [rip + 0x1ca8bef] ; RIP_RVA=0x1eae740
00205B51  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205B55  4c8d05ecfa8d01                 lea       r8, [rip + 0x18dfaec] ; RIP_RVA=0x1ae5648
00205B5C  488bcd                         mov       rcx, rbp
00205B5F  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205B64  488b9080000000                 mov       rdx, qword ptr [rax + 0x80]
00205B6B  4885d2                         test      rdx, rdx
00205B6E  480f45da                       cmovne    rbx, rdx
00205B72  488d15cffa8d01                 lea       rdx, [rip + 0x18dfacf] ; RIP_RVA=0x1ae5648
00205B79  4c8bcb                         mov       r9, rbx
00205B7C  e81fc79c00                     call      0xbd22a0
00205B81  488bd5                         mov       rdx, rbp
00205B84  488bcb                         mov       rcx, rbx
00205B87  e814e30400                     call      0x253ea0
00205B8C  488bcd                         mov       rcx, rbp
00205B8F  e83cd59c00                     call      0xbd30d0
00205B94  4488350d1bc501                 mov       byte ptr [rip + 0x1c51b0d], r14b ; RIP_RVA=0x1e576a8
00205B9B  488d1dfe1ac501                 lea       rbx, [rip + 0x1c51afe] ; RIP_RVA=0x1e576a0
00205BA2  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205BA6  4c8d058bfc8d01                 lea       r8, [rip + 0x18dfc8b] ; RIP_RVA=0x1ae5838
00205BAD  488bcd                         mov       rcx, rbp
00205BB0  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205BB5  488b9090000000                 mov       rdx, qword ptr [rax + 0x90]
00205BBC  4885d2                         test      rdx, rdx
00205BBF  480f45da                       cmovne    rbx, rdx
00205BC3  488d156efc8d01                 lea       rdx, [rip + 0x18dfc6e] ; RIP_RVA=0x1ae5838
00205BCA  4c8bcb                         mov       r9, rbx
00205BCD  e8cec69c00                     call      0xbd22a0
00205BD2  488bd5                         mov       rdx, rbp
00205BD5  488bcb                         mov       rcx, rbx
00205BD8  e893730b00                     call      0x2bcf70
00205BDD  488bcd                         mov       rcx, rbp
00205BE0  e8ebd49c00                     call      0xbd30d0
00205BE5  448835fc1ac501                 mov       byte ptr [rip + 0x1c51afc], r14b ; RIP_RVA=0x1e576e8
00205BEC  488d1ded1ac501                 lea       rbx, [rip + 0x1c51aed] ; RIP_RVA=0x1e576e0
00205BF3  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205BF7  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205BFC  488b9098000000                 mov       rdx, qword ptr [rax + 0x98]
00205C03  4885d2                         test      rdx, rdx
00205C06  480f45da                       cmovne    rbx, rdx
00205C0A  4c8bcb                         mov       r9, rbx
00205C0D  4c8d05e4fb8d01                 lea       r8, [rip + 0x18dfbe4] ; RIP_RVA=0x1ae57f8
00205C14  488d15ddfb8d01                 lea       rdx, [rip + 0x18dfbdd] ; RIP_RVA=0x1ae57f8
00205C1B  488bcd                         mov       rcx, rbp
00205C1E  e87dc69c00                     call      0xbd22a0
00205C23  488bd5                         mov       rdx, rbp
00205C26  488bcb                         mov       rcx, rbx
00205C29  e8926d0b00                     call      0x2bc9c0
00205C2E  488bcd                         mov       rcx, rbp
00205C31  e89ad49c00                     call      0xbd30d0
00205C36  4488351b8dca01                 mov       byte ptr [rip + 0x1ca8d1b], r14b ; RIP_RVA=0x1eae958
00205C3D  488d1d0c8dca01                 lea       rbx, [rip + 0x1ca8d0c] ; RIP_RVA=0x1eae950
00205C44  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205C48  4c8d0519fb8d01                 lea       r8, [rip + 0x18dfb19] ; RIP_RVA=0x1ae5768
00205C4F  488bcd                         mov       rcx, rbp
00205C52  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205C57  488b90a0000000                 mov       rdx, qword ptr [rax + 0xa0]
00205C5E  4885d2                         test      rdx, rdx
00205C61  480f45da                       cmovne    rbx, rdx
00205C65  488d15fcfa8d01                 lea       rdx, [rip + 0x18dfafc] ; RIP_RVA=0x1ae5768
00205C6C  4c8bcb                         mov       r9, rbx
00205C6F  e82cc69c00                     call      0xbd22a0
00205C74  488bd5                         mov       rdx, rbp
00205C77  488bcb                         mov       rcx, rbx
00205C7A  e831240d00                     call      0x2d80b0
00205C7F  488bcd                         mov       rcx, rbp
00205C82  e849d49c00                     call      0xbd30d0
00205C87  448835ca8bca01                 mov       byte ptr [rip + 0x1ca8bca], r14b ; RIP_RVA=0x1eae858
00205C8E  488d1dbb8bca01                 lea       rbx, [rip + 0x1ca8bbb] ; RIP_RVA=0x1eae850
00205C95  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205C99  4c8d0578fb8d01                 lea       r8, [rip + 0x18dfb78] ; RIP_RVA=0x1ae5818
00205CA0  488bcd                         mov       rcx, rbp
00205CA3  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205CA8  488b9088000000                 mov       rdx, qword ptr [rax + 0x88]
00205CAF  4885d2                         test      rdx, rdx
00205CB2  480f45da                       cmovne    rbx, rdx
00205CB6  488d155bfb8d01                 lea       rdx, [rip + 0x18dfb5b] ; RIP_RVA=0x1ae5818
00205CBD  4c8bcb                         mov       r9, rbx
00205CC0  e8dbc59c00                     call      0xbd22a0
00205CC5  488bd5                         mov       rdx, rbp
00205CC8  488bcb                         mov       rcx, rbx
00205CCB  e8406d0b00                     call      0x2bca10
00205CD0  488bcd                         mov       rcx, rbp
00205CD3  e8f8d39c00                     call      0xbd30d0
00205CD8  488bcf                         mov       rcx, rdi
00205CDB  e8c0460601                     call      0x126a3a0
00205CE0  448835318dca01                 mov       byte ptr [rip + 0x1ca8d31], r14b ; RIP_RVA=0x1eaea18
00205CE7  488d1d228dca01                 lea       rbx, [rip + 0x1ca8d22] ; RIP_RVA=0x1eaea10
00205CEE  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205CF2  4c8d05a7f98d01                 lea       r8, [rip + 0x18df9a7] ; RIP_RVA=0x1ae56a0
00205CF9  488bcd                         mov       rcx, rbp
00205CFC  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205D01  488b90a8000000                 mov       rdx, qword ptr [rax + 0xa8]
00205D08  4885d2                         test      rdx, rdx
00205D0B  480f45da                       cmovne    rbx, rdx
00205D0F  488d158af98d01                 lea       rdx, [rip + 0x18df98a] ; RIP_RVA=0x1ae56a0
00205D16  4c8bcb                         mov       r9, rbx
00205D19  e882c59c00                     call      0xbd22a0
00205D1E  488bd5                         mov       rdx, rbp
00205D21  488bcb                         mov       rcx, rbx
00205D24  e8b7910500                     call      0x25eee0
00205D29  488bcd                         mov       rcx, rbp
00205D2C  e89fd39c00                     call      0xbd30d0
00205D31  488bcb                         mov       rcx, rbx
00205D34  e8674aebff                     call      0xba7a0
00205D39  448835d819c501                 mov       byte ptr [rip + 0x1c519d8], r14b ; RIP_RVA=0x1e57718
00205D40  488d1dc919c501                 lea       rbx, [rip + 0x1c519c9] ; RIP_RVA=0x1e57710
00205D47  488b4658                       mov       rax, qword ptr [rsi + 0x58]
00205D4B  4c8d0546fb8d01                 lea       r8, [rip + 0x18dfb46] ; RIP_RVA=0x1ae5898
00205D52  488bcd                         mov       rcx, rbp
00205D55  4489742420                     mov       dword ptr [rsp + 0x20], r14d
00205D5A  488b90b0000000                 mov       rdx, qword ptr [rax + 0xb0]
00205D61  4885d2                         test      rdx, rdx
00205D64  480f45da                       cmovne    rbx, rdx
00205D68  488d1529fb8d01                 lea       rdx, [rip + 0x18dfb29] ; RIP_RVA=0x1ae5898
00205D6F  4c8bcb                         mov       r9, rbx
00205D72  e829c59c00                     call      0xbd22a0
00205D77  488bd5                         mov       rdx, rbp
00205D7A  488bcb                         mov       rcx, rbx
00205D7D  e8be940500                     call      0x25f240
00205D82  488bcd                         mov       rcx, rbp
00205D85  e846d39c00                     call      0xbd30d0
00205D8A  488bcb                         mov       rcx, rbx
00205D8D  488b5c2440                     mov       rbx, qword ptr [rsp + 0x40]
00205D92  488b6c2448                     mov       rbp, qword ptr [rsp + 0x48]
00205D97  488b742450                     mov       rsi, qword ptr [rsp + 0x50]
00205D9C  488b7c2458                     mov       rdi, qword ptr [rsp + 0x58]
00205DA1  4883c430                       add       rsp, 0x30
00205DA5  415e                           pop       r14
00205DA7  e9f449ebff                     jmp       0xba7a0
