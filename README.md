# NzbLiteClient

NzbLiteClient is the official software from https://www.nzblite.com

It implements NzbLite format which is an open source file format to simplify uploading and downloading to newsgroups.

NzbLiteClient is a powerfull and fast tool to backup and restore your data on the UseNet network.

It was developed to answer to various problematics:

* Full NzbLite format implementation
* Backup a huge quantity of data
* Restore easily your data
* Secure your data
* Share nzblite link with friends and social networks
* Easy use with minimal settings

NzbLiteClient is easy to setup and to use :-)

## Requirements

NzbLiteClient is fully compatible with Windows and Linux platforms. It required only .Net Core and par2 to work.

## Features

* Backup thousand files to UseNet
* Fast upload and multiconnection to usenet
* Check and repost missing or failed article post
* Integrated file chunker and splitter
* Data encryption for better security
* Generated PAR2 parity files for each file backuped
* Multi mode: Backup, Restore, Upload, Download, Sync and Clean
* Database auto save
* Checksum of each file
* Console mode application
* Low resource consumption (cpu, disk, memory and network)
* Api interaction with https://www.nzblite.com/indexer.php to synchronise your files and to perform automatic downloads
* Socks and Proxy support
* Easy to setup and to use
* And much more :-)

## Installation

Download NzbLiteClient in Release section and unzip the content into a folder of your choice (like C:\NzbLiteClient).

Edit the file config.json with your favorite text editor and fill the parameters.

Run NzbLiteClient.exe (for linux: dotnet NzbLiteClient.dll) and that's all !

## Parameters

You need to overwrite some settings in file config.json

### Upload Settings

These settings apply for upload file:

* CleanNames: true,
* FileMinSize: minimum filesize to filter for files to backup
* Filters: array of regex to filter files to backup
* RemoveMissing: remove missing files from database,
* PercentSuccess: percentage of success chunk upload to valid file,

### Api Settings

These settings are required to synchronise your uploads and to automate download using NzbLite.com Indexer:

* ApiKey: your apikey to synchronize your NzbLite links and to automate downloads using our powerfull Indexer
* ApiSyncAuto: to enable / disable auto synchronisation to NzbLite.com Indexer
* ApiSyncUrl: url for synchronisation

ApiSyncAuto and ApiSyncUrl are only used by our Uploaders Team. It requires a special access permission, so contact us if you want to join our Uploaders Team.

### NewsGroup Settings

These settings are required to connect to your usenet server:

* UsenetUsername: your newsgroup username
* UsenetPassword: your newsgroup password
* UsenetServer: newsgroup server ip address to use
* UsenetUseSSL: enable SSL
* UsenetPort: newsgroup port to use
* UsenetSlots: max conns to use for upload and download
* UsenetNewsgroup: group to post your files,

### Proxy Settings

NzbLiteClient supports proxy. You could enable it with these settings:

* ProxyEnabled: enable / disable proxy
* ProxyType: http, socks4, socks5
* ProxyServer: proxy server ip address to use
* ProxyPort: proxy server port to use
* ProxyUsername: proxy server username
* ProxyPassword: proxy server password

### Parity Settings

NzbLiteClient automatically generates parity files. You could edit this parameters to tweak it:

* ParRedundancy: percentage of redundancy to create parity files (min: 5% - default value: 15%)
* ParPath: par2 binary path
* ParThreads: number of threads assigned to par2 process (0 for no limit)

### Folders Settings

You could define multiple folders to scan. Then each file matching filtering rules will be uploaded to Usenet:

* Path: folder path
* Encrypted: enable / disable fast file encryption
* FileNamingRule: defines file name. Possible value: PARENTNAME, FILENAME or FULLPATH
* Tag: a custom tag information
* Category: file category.For SyncAuto only these value are valid: Movies, TvShows, Animes
* Lang: file language. For SyncAuto only these value are valid: EN, FR, DE or ES

## How NzbLiteClient works

NzbLiteClient is a software which run in console (known as terminal or background mode too) or in batch (background) command line.

NzbLiteClient (console or command line) purposes differents modes

### Backup

Backup mode allows you to backup your data specified in the config.json file. First each file to backup is encrypted and chunked. Next, parity files are generated. Finally, chunk files and parity files are posted to your newsgroup server.

If an upload is successful (percentage of good chunk uploads superior to parameter PercentSuccess), a NzbLite link is generated (in hexadecimal representation) and the database is updated.

When NzbLiteClient is used in backup mode it adds only new file. It never remove entries from is database. So if you delete a folder entry in your settings.conf, previous uploaded files aren't removed.

### Restore

Restore mode allows you to recovery your data. You only need to specify which directory you want to recover and NzbLiteClient downloads your files from your newsgroup server. It automatically joins, decrypts and checks downloaded files.

If a download is successful, recovery file is moved into "download" directory.

### Upload single file

This mode permits to upload only a single file specifying a filepath. This mode is very useful to upload a file quickly and share its NzbLite link with friends.

### Download single file

This mode allows you to download a single file using its NzbLite link.

### Sync files to NzbLite.com

This mode could be used only if you are a member of our Uploader Team. All file uploaded and not yet synced are sent to NzbLite.com Indexer.

### Convert Link to Nzb

This mode converts a NzbLite link into Nzb file (xml).

### Clean local datatabase

This mode removes entries without NzbLite link from local database.

## Command line use

For some reasons it could be interesting to use NzbLiteClient in command line, per example to program multiple downloads using a batch file.

Available command line args:

```dos
-b: Backup
-r: Restore
-u path: Upload a file
-d NzbLiteLink [outputDir]: Download a file using its NzbLite link
-s: Synchronize uploaded files to NzbLite Indexer (Uploader Team restricted)
-c NzbLiteLink [outputDir]: convert a NzbLite link into a Nzb file
```

## Screenshot

![NzbLiteClient](https://github.com/jhdscript/NzbLiteClient/blob/master/nzbliteclient.png)

As you can see in this screenshot, NzbLiteClient is very performant and it could be used to backup or restore large volume of data.

## Technical information

NzbLiteClient is develop in C# because .Net Core Framework is fast, easy to learn and multi platform. Windows and Linux are supported, x86 and x64 too :-)

### Logical Tree

The logical tree explains how is structured NzbLiteClient. [D] is for directory and [F] for file.

```
[D] NzbLiteClient Folder
---[D] download: folder for download output
---[D] logs: logs folder
---[D] temp: temporary folder for processing
---[F] config.json: NzbLiteClient configuration file
---[F] database.db: local database
---[F] log4net.config: logging config file
---[F] log4net.dll: logging extension
---[F] Newtonsoft.Json.dll: JSON library
---[F] NzbLiteClient.deps.json: dependancies config file
---[F] NzbLiteClient.dll: main program library
---[F] NzbLiteClient.exe: main program executable (for Windows only)
---[F] NzbLiteClient.pdb: debug file
---[F] NzbLiteClient.runtimeconfig.json: runtime config file
---[F] par2.exe: par2cmdline (for Windows only)
---[F] ProxyLib.dll: proxy support extension
```

### Processing

NzbLiteClient operation is easy and the code is optimised and designed for performance. Each mode runs only 1 processing thread, except when it uses par2 to generate or check parities.

In Upload / Backup mode, the thread starts with Chunk / Encrypt / Par files. After the thread is used for posting data to usenet. If posting is successful, NzbLite link is generated and stored into database. If posting is failed, file is skipped and it will be processed later.

In Download / Restore mode, the thread is used for download every file chunks from your newsgroup server. When all chunks are downloaded, checksum is verified and if checksum mismatch, parity files are downloaded. Then par2 repairs the file and checksum is verified another time. If it's valid, file is move to "download" directory.

In Backup Mode and Restore Mode, another thread is started to scan directories and to add new files to process in database.

A last thread is used to autosave the local database.

### Database

Local database is very easy to reuse and to understand. It's in JSON format so you can open it with a simple NotePad.

## Prerequistes

To run correctly NzbLiteClient, you have to install:

* Microsoft .Net Core 3.1 or superior
* Par2CmdLine (GNU General Public License v2.0 - Copyright (c) 2017 Ike Devolder)

## Known issues

If your usenet provider use HW Media backbone, you could have some issues after posting a large volume of data. This provider is very strange and after a couple of hour of posting it skip a lot of posts.

I recommand you to use a newsgroup provider in another backbone for posting (cf. UseNet tree).

![Usenet Tree](https://github.com/jhdscript/nzbliteclient/blob/master/usenet-tree.svg)

## Changelog

### Version 1.0.0 (first release) - 20200124

* Backup mode is fully working
* Restore mode is fully working
* Upload single file is fully working
* Download single file is fully working
* Speeds improvement for encryption and chunk
* Fast checksum calculation
* I/O Optimisation to reduce filesystem access
* CPU consumption reduced
* Memory consumption reduced

### Version 0.0.0 (pre version) - 20191201

* First Batch of NzbLiteClient
* All program architecture is defined
* Database designed
* Development language is set to C#

## Question / Support / Donation

For any question I recommand you to visit https://www.nzblite.com or to open a GitHub issue.

NzbLiteClient is totally free, but you can help us with a small donation:

* BTC: 1EGeXCdvWz2RdhcPnPuStg8s2TFs18Yi5x
* LTC: LY8uA7JDbg8kQBnLp6DkqEuFD1zNqKayQj
* TRX: TX4xiCK4aS84vmTL9CWK141EFCg53wTtmY
* ETH: 0xf47d7b0c1efa3aa7df0cd5ffb60b6fbd085187eb
* ETC: 0xf47d7b0c1efa3aa7df0cd5ffb60b6fbd085187eb

Paypal donation could be sent to: jhdscript at gmail.com

## License

NzbLiteClient (Release and Source code) is distributed under GNU General Public License V3

Copyright (c) 2019 jhdscript at gmail.com (www.nzblite.com)
