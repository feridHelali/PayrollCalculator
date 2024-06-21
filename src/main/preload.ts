import { sectorialJointAgreementProps } from "../types/sectorialAgreementProps";

const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  fetchAgreements: () => ipcRenderer.invoke('fetch-agreements'),
  fetchAgreementById: (id:number) => ipcRenderer.invoke('fetch-agreement-by-id', id),
  createAgreement: (agreement:sectorialJointAgreementProps) => ipcRenderer.invoke('create-agreement', agreement),
  updateAgreement: (agreement:sectorialJointAgreementProps) => ipcRenderer.invoke('update-agreement', agreement),
  deleteAgreement: (id:number) => ipcRenderer.invoke('delete-agreement', id),
});
