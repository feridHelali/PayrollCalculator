import 'reflect-metadata';
import path from 'path';
import { app, BrowserWindow, Menu, MenuItemConstructorOptions, ipcMain } from 'electron';
import initializeDatabase from './db/initializeDatabase';
import { SecrorialJointAgreementService } from './services/SectorialJointAgreementService';



function createWindow() {
  const mainWindow = new BrowserWindow({
    width: 1200,
    height: 1000,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: path.join(__dirname, 'preload.js')
    }
  });

  const startUrl = process.env.ELECTRON_START_URL || 'http://localhost:9000';

  mainWindow.loadURL(startUrl);
  mainWindow.webContents.openDevTools();

  mainWindow.on('closed', () => {
    app.quit();
  });
}

app.on('ready', async () => {
  const template: MenuItemConstructorOptions[] = [
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

  try {
    await initializeDatabase();

    const agreementService = new SecrorialJointAgreementService();

    // Register IPC handlers only after the data source is initialized
    ipcMain.handle('fetch-agreements', async () => {
      return await agreementService.getAllSectorialJointAgreements();
    });

    ipcMain.handle('fetch-agreement-by-id', async (event, id) => {
      return await agreementService.getSectorialJointAgreementById(id);
    });

    ipcMain.handle('create-agreement', async (event, agreement) => {
      return await agreementService.createSectorialJointAgreement(agreement);
    });

    ipcMain.handle('update-agreement', async (event, agreement) => {
      return await agreementService.updateSectorialJointAgreement(agreement.id, agreement);
    });

    ipcMain.handle('delete-agreement', async (event, id) => {
      await agreementService.deleteSectorialJointAgreement(id);
      return id;
    });

    createWindow();
  } catch (err) {
    console.error('Error during app initialization:', err);
  }
});

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
