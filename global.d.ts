// global.d.ts
export {};

declare global {
  interface Window {
    electronAPI: {
      fetchAgreements: () => Promise<any>;
      fetchAgreementById: (sectorialJointAgreementId: string) => Promise<any>;
      createAgreement: (agreement: any) => Promise<any>;
      updateAgreement: (agreement: any) => Promise<any>;
      deleteAgreement: (id: number) => Promise<any>;
      fetchSalaryTablesByAgreementId: (agreementId: number) => Promise<any>;
      fetchAllSalaryTables: () => Promise<any>;
      fetchSalaryTableById: (id: number) => Promise<any>;
      createSalaryTable: (salaryTable: any) => Promise<any>;
      updateSalaryTable: (salaryTable: any) => Promise<any>;
      deleteSalaryTable: (id: number) => Promise<any>;
    };
  }
}


interface ElectronAPI {
  fetchAgreements: () => Promise<any>;
      fetchAgreementById: (sectorialJointAgreementId: string) => Promise<any>;
      createAgreement: (agreement: any) => Promise<any>;
      updateAgreement: (agreement: any) => Promise<any>;
      deleteAgreement: (id: number) => Promise<any>;
      fetchSalaryTablesByAgreementId: (agreementId: number) => Promise<any>;
      fetchAllSalaryTables: () => Promise<any>;
      fetchSalaryTableById: (id: number) => Promise<any>;
      createSalaryTable: (salaryTable: any) => Promise<any>;
      updateSalaryTable: (salaryTable: any) => Promise<any>;
      deleteSalaryTable: (id: number) => Promise<any>;
}

interface Window {
  electronAPI: ElectronAPI;
}