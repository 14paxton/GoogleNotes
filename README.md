# [Chromium Docs](https://chromium.googlesource.com/chromium/src/+/master/docs/user_data_dir.md)

> get profile info
```
chrome://version/
```

# Extensions 
## File Locations 
Windows XP: C:\Documents and Settings\%USERNAME%\Local Settings\Application Data\Google\Chrome\User Data\Default\Extensions\<Extension ID>

Windows 10/8/7/Vista: C:\Users\%USERNAME%\AppData\Local\Google\Chrome\User Data\Default\Extensions\<Extension ID>

macOS: ~/Library/Application Support/Google/Chrome/Default/Extensions/<Extension ID>

Mac Path: /Users/<username>/Library/Application Support/Google/Chrome/Default/Extensions/<Extension ID> Extension ID can be found at chrome://extensions (with Developer Mode enabled) 

Linux: ~/.config/google-chrome/Default/Extensions/<Extension ID>

Ubuntu: ~/.config/google-chrome/Default/Extensions

Chrome OS: /home/chronos/Extensions/<Extension ID>

You can copy the extension folder and drop it on a USB or in a network drive.

To install
Open Chrome and go to chrome://extensions.
Make sure Developer Mode is checked.
Click Load Unpacked Extension....
Find your copied directory and click Open.
