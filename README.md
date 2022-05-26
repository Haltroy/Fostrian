# Fostrian

Fostrian is a nodal data structure format created for use with [Foster](https://github.com/haltroy/Foster).
Each data can represent any single type of data (string, integer, long, float etc.) and can have multiple sub nodes that can hold more data.

- Latest Long-Term Supported Version: [`1.0.2`](https://github.com/haltroy/Fostrian/tree/1.0.2)
- Latest Recommended Version: [`1.0.2`](https://github.com/haltroy/Fostrian/tree/1.0.2)
- In Development Version: [`1.1`](https://github.com/haltroy/Fostrian/tree/1.0)

## Installation & Usage

Fostrian is a C# library that can be used in any .NET project that supports [.NET Standard 2.0](https://github.com/dotnet/standard/blob/v2.1.0/docs/versions/netstandard2.0.md#platform-support). We recommend installing Fostrian from NuGet but we also share Fostrian binaries in GitHub Releases.

- [NuGet Package](https://nuget.org/packages/Fostrian)
- [GitHub Releases](https://github.com/haltroy/Fostrian/Releases)

After installing Fostrian, add `LibFoster` to your usings:

- VB: add `Imports LibFoster` to top
- C#: add `using LibFoster;` to top

A Fostrian formatted file contains sub nodes that are named `FostrianNode` in code. Fostrian nodes should act similar to Lists or arrays. A Fostrian node can hold a byte array that contains data as the name `Data`. You can use the conversion methods such as `DataAsString()` to see the data. In order to add more data, you can use the `Add()` method with different data types that are already implemented. You can use certain List methods such as `Count`, `Find`, `Remove`. Each node has a root node.

Fostrian keeps the encoding of all strings inside a global cane where the data start and end byte marks are also stored, which is the beginning of the file.

## Structure

A Fostrian formatted file looks like this under a Hexadecimal Editor:

`[Data Start Mark Byte] [Data End Mark Byte] [String Encoding Mark] [[Start Mark Byte] [Data itself] [End Mark Byte] [Int32 of how many sub nodes does this data has] this repeats itself for each node]`

Example:

`0x02 0x03 0x04 0x02 00x54 0x00 0x65 0x00 0x73 0x00 0x74 0x00 0x03 0x01 0x02 0x41 0x00 0x6E 0x00 0x6F 0x00 0x74 0x00 0x68 0x00 0x65 0x00 0x72 0x00 0x20 0x00 0x54 0x00 0x65 0x00 0x73 0x00 0x74 0x00 0x03 0x00`

Which contains:

- Root
  - `Test`
    - `Another Test`

## Releases

Please visit [VERSIONS.md](https://github.com/haltroy/Fostrian/blob/main/VERSIONS.md) for more information.

| Version Tree                                          | Date              | Status                   | .NET Standard | Support            | LTS Status            |
| ----------------------------------------------------- | ----------------- | ------------------------ | ------------- | ------------------ | --------------------- |
| [`1.1.x`](https://github.com/haltroy/Fostrian/blob/main/VERSIONS.md#1.1.x) | _No Certain Date_ | _In Development_         | 2.0           | :clock12:          | None                  |
| [`1.0.x`](https://github.com/haltroy/Fostrian/blob/main/VERSIONS.md#1.0.x) | 25 May 2022       | **Latest LTS Supported** | 2.0           | :heavy_check_mark: | Ends in `25 May 2024` |
