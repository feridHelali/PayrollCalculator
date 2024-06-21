// global.d.ts
export {};

declare global {
  interface Window {
    electronAPI: {
      fetchAgreements: () => Promise<any>;
      fetchAgreementById: (sectorialJointAgreementId: number) => Promise<any>;
      createAgreement: (agreement: any) => Promise<any>;
      updateAgreement: (agreement: any) => Promise<any>;
      deleteAgreement: (id: number) => Promise<any>;
    };
  }
}


interface ElectronAPI {
  fetchAgreements: () => Promise<any>;
  fetchAgreementById: (sectorialJointAgreementId: number) => Promise<any>;
  createAgreement: (agreement: any) => Promise<any>;
  updateAgreement: (agreement: any) => Promise<any>;
  deleteAgreement: (id: number) => Promise<any>;
}

interface Window {
  electronAPI: ElectronAPI;
}