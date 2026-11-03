import { sectorialJointAgreementProps } from "../types/sectorialAgreementProps";

const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  fetchAgreements: () => ipcRenderer.invoke('fetch-agreements'),
  fetchAgreementById: (id: number) => ipcRenderer.invoke('fetch-agreement-by-id', id),
  createAgreement: (agreement: sectorialJointAgreementProps) => ipcRenderer.invoke('create-agreement', agreement),
  updateAgreement: (agreement: sectorialJointAgreementProps) => ipcRenderer.invoke('update-agreement', agreement),
  deleteAgreement: (id: number) => ipcRenderer.invoke('delete-agreement', id),

  fetchAllSalaryTables: () => ipcRenderer.invoke('fetch-all-salary-tables'),
  fetchSalaryTableById: (id: number) => ipcRenderer.invoke('fetch-salary-table-by-id', id),
  fetchSalaryTablesByAgreementId: (agreementId: number) => ipcRenderer.invoke('fetch-salary-tables-by-agreement-id', agreementId),
  createSalaryTable: (salaryTable: any) => ipcRenderer.invoke('create-salary-table', salaryTable),
  updateSalaryTable: (salaryTable: any) => ipcRenderer.invoke('update-salary-table', salaryTable),
  deleteSalaryTable: (id: number) => ipcRenderer.invoke('delete-salary-table', id),
 
  fetchAffairs: () => ipcRenderer.invoke('fetch-affairs'),
  fetchAffairById: (id: number) => ipcRenderer.invoke('fetch-affair-by-id', id),
  createAffair: (affair: any) => ipcRenderer.invoke('create-affair', affair),
  updateAffair: (affair: any) => ipcRenderer.invoke('update-affair', affair),
  deleteAffair: (id: number) => ipcRenderer.invoke('delete-affair', id),
});
