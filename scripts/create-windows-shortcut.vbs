' ==============================================================================
' Sensei Study Engine - Desktop Shortcut Creator for Windows
' ==============================================================================
Set WshShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

strScriptDir = fso.GetParentFolderName(WScript.ScriptFullName)
strRootDir = fso.GetParentFolderName(strScriptDir)
strDesktop = WshShell.SpecialFolders("Desktop")

strShortcutPath = strDesktop & "\Sensei Study Engine.lnk"
Set oLink = WshShell.CreateShortcut(strShortcutPath)

oLink.TargetPath = strScriptDir & "\sensei-windows.bat"
oLink.WorkingDirectory = strRootDir
oLink.Description = "Sensei - Personal AI Study Coach & Engineering Platform"

' Icon if available
strIconPath = strRootDir & "\frontend\public\icons\sensei.ico"
If fso.FileExists(strIconPath) Then
    oLink.IconLocation = strIconPath
End If

oLink.WindowStyle = 7 ' Minimized launcher window
oLink.Save

WScript.Echo "Created Sensei shortcut on Desktop: " & strShortcutPath
