// global.d.ts
export { };


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
export type MockElectronAPI = {
  fetchAllSalaryTables: jest.Mock<Promise<any>, any[]>;
  fetchSalaryTableById: jest.Mock<Promise<any>, [number]>;
  fetchSalaryTablesByAgreementId: jest.Mock<Promise<any>, [number]>;
  createSalaryTable: jest.Mock<Promise<any>, [any]>;
  updateSalaryTable: jest.Mock<Promise<any>, [any]>;
  deleteSalaryTable: jest.Mock<Promise<any>, [number]>;
  fetchAgreements: jest.Mock<Promise<any>, any[]>;
  fetchAgreementById: jest.Mock<Promise<any>, [number]>;
  createAgreement: jest.Mock<Promise<any>, [any]>;
  updateAgreement: jest.Mock<Promise<any>, [any]>;
  deleteAgreement: jest.Mock<Promise<any>, [number]>;
};


declare global {
  interface Window {
    electronAPI: ElectronAPI;
    mockElectronAPI: MockElectronAPI;
  }
}

