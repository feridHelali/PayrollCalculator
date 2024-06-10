import 'reflect-metadata';
import { app, BrowserWindow,Menu,MenuItemConstructorOptions,ipcMain  } from 'electron';
import { ConventionCollectioveService } from '../services/ConventionCollectiveService';


function createWindow() {
  const mainWindow = new BrowserWindow({
    width: 800,
    height: 600,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: false,
    }
  });

  const startUrl = process.env.ELECTRON_START_URL || 'http://localhost:9000';

  mainWindow.loadURL(startUrl);

  mainWindow.on('closed', () => {
    app.quit();
  });
}



app.on('ready', () => {
  const template:MenuItemConstructorOptions[] = [
    {
      label: 'الملف',
      submenu: [
        { label: 'فتح الملف' },
        { label: 'حفظ الملف' },
        { type: 'separator' },
        { label: 'خروج', role: 'quit' }
      ]
    },
    // Add more menu items as needed
  ];

  const menu = Menu.buildFromTemplate(template);
  Menu.setApplicationMenu(menu);
});


app.on('ready', createWindow);

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
});

// IPC Setup in Electron Main Process
ipcMain.handle('fetch-conventions', async (event:any, args:any) => {
 console.log('fetchConventions Trigred from GUI')
 return [];
});