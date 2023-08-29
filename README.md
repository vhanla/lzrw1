# lzrw1
LZRW1 port from Pascal to C# from [Danny Heijl's Delphi implementation](https://www.sac.sk/download/pack/tlzrw1.zip), but without the added headers.

Usage:

`lzrw1.exe <compress|decompress> <inputfile> <outputfile>`



Known Bugs 🐛🔧:
- Needs fix for big files (bigger than 32K) 🙈

Sources&Literature:
- [Williams, R.N., "An Extremely Fast Ziv-Lempel Data Compression Algorithm", Data Compression Conference 1991 (DCC'91),  8-11 April , 1991, Snowbird, Utah, pp.362-371, IEEE reference code: TH0373-1/91/0000/0362/$01.00. ](http://www.ross.net/compression/lzrw1.html)
- [LZRW variants](https://en.wikipedia.org/wiki/LZRW)
- [LZRW1 Encoder Core](https://opencores.org/websvn/filedetails?repname=lzrw1-compressor-core&path=%2Flzrw1-compressor-core%2Ftrunk%2Fdocumentation%2FLZRW1+Compressor+Core+V1.0.pdf)
- [Data Compression Explained](https://mattmahoney.net/dc/dce.html#Section_53)
- [python-lzrw1-kh](https://github.com/nmantani/python-lzrw1-kh)
