# Native Project Picker Automation Diagnosis

The installed Workstation's **Choose project** command opened the real Windows folder picker titled **Choose a project folder for review**. The dialog was confirmed by passive desktop captures, including its **Folder** text field and **Select Folder** button.

The dialog did not appear in the first top-level UI Automation window listing, despite being visibly open. Its own Automation tree identifies the Folder-related control hierarchy as panes `1090` and `1152`, rather than an exposed editable ValuePattern. A clipboard attempt did not visibly populate the Folder field. No folder has been selected and the Workstation candidate has not been changed by these attempts.

The next limited probe will use normal raw keystrokes only while the visible Folder field has the caret, followed by a visual check before any Select Folder action.
