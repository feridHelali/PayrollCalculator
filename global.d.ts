// global.d.ts
export {};

declare global {
  interface Window {
    electronAPI: {
      fetchAgreements: () => Promise<any>;
      fetchAgreementById: (sectorialJointAgreementId: string) => Promise<any>;
      createAgreement: (agreement: any) => Promise<any>;
      updateAgreement: (agreement: any) => Promise<any>;
      deleteAgreement: (id: string) => Promise<any>;
      fetchSalaryTable: (id: string) => Promise<any>;
      createSalaryTable: (salaryTable: any) => Promise<any>;
      updateSalaryTable: (salaryTable: any) => Promise<any>;
      deleteSalaryTable: (id: string) => Promise<any>;
    };
  }
}


interface ElectronAPI {
  fetchAgreements: () => Promise<any>;
  fetchAgreementById: (sectorialJointAgreementId: string) => Promise<any>;
  createAgreement: (agreement: any) => Promise<any>;
  updateAgreement: (agreement: any) => Promise<any>;
  deleteAgreement: (id: string) => Promise<any>;
  fetchSalaryTable: (id: string) => Promise<any>;
  createSalaryTable: (salaryTable: any) => Promise<any>;
  updateSalaryTable: (salaryTable: any) => Promise<any>;
  deleteSalaryTable: (id: string) => Promise<any>;
}

interface Window {
  electronAPI: ElectronAPI;
}