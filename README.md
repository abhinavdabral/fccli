# Folder Compare CLI (fccli)

##### Version 0.1 (Initial Beta Release)

## About

Folder Compare CLI is intended to compare files across two different folders that contains same files (file names), mostly in the case of backups. It compares the files by computing checksum of files at either locations; if the comparison fails, the filename is logged in the console screen along with a few others details.

## How To Use

- Place fccli.exe at any location
- (Optionally) Append the path of the location of fccli.exe to your environment variable PATH, so that you can execute fccli from everywhere.
- To compare the current folder with another folder, use :
  - `fccli "<destination folder>"`
- To compare two folders using absolute paths, use :
  - `fccli "<source folder>" "<destination folder>"`

> **NOTE : **When specified folder path, make sure to enclose them within " " to avoid the problems caused by spaces in the path.

## Author

Abhinav Dabral (abhinavdabral)

## License

MIT

## Change Log

- v0.1 (25 Nov, 2016)
  - Initial Commit



