import { configureStore } from '@reduxjs/toolkit';
import salaryTableReducer, {
  fetchAllSalaryTables,
  fetchSalaryTableById,
  fetchSalaryTablesByAgreementId,
  createSalaryTable,
  updateSalaryTable,
  deleteSalaryTable,
  switchToUpdateMode,
  switchToCreateMode,
} from '../../src/renderer/redux/sectorialJointAgreement/salaryTableSlice';
import { SalaryTableProps } from '../../src/types/salaryTableProps';
import '@testing-library/jest-dom';
import { jest } from '@jest/globals';
import { MockElectronAPI } from '../../global.d';




// Mocking the Electron API
window.mockElectronAPI = {
  fetchAllSalaryTables: jest.fn() as jest.MockedFunction<MockElectronAPI['fetchAllSalaryTables']>,
  fetchSalaryTableById: jest.fn() as jest.MockedFunction<MockElectronAPI['fetchSalaryTableById']>,
  fetchSalaryTablesByAgreementId: jest.fn() as jest.MockedFunction<MockElectronAPI['fetchSalaryTablesByAgreementId']>,
  createSalaryTable: jest.fn() as jest.MockedFunction<MockElectronAPI['createSalaryTable']>,
  updateSalaryTable: jest.fn() as jest.MockedFunction<MockElectronAPI['updateSalaryTable']>,
  deleteSalaryTable: jest.fn() as jest.MockedFunction<MockElectronAPI['deleteSalaryTable']>,
  fetchAgreements: jest.fn() as jest.MockedFunction<MockElectronAPI['fetchAgreements']>,
  fetchAgreementById: jest.fn() as jest.MockedFunction<MockElectronAPI['fetchAgreementById']>,
  createAgreement: jest.fn() as jest.MockedFunction<MockElectronAPI['createAgreement']>,
  updateAgreement: jest.fn() as jest.MockedFunction<MockElectronAPI['updateAgreement']>,
  deleteAgreement: jest.fn() as jest.MockedFunction<MockElectronAPI['deleteAgreement']>,
} as MockElectronAPI;





const mockSalaryTable: SalaryTableProps = {
  agreementId: 1,
  numeroTable: '123',
  type: 'Type',
  consernedEmployee: 'Employee',
  beginningDateOfApplication: '2022-01-01',
  endDateOfApplication: '2022-12-31',
  degrees: [1],
  workingAges: [1],
  categories: ['Category 1'],
  salaryTableId: 1,
};

describe('salaryTableSlice', () => {
  let store: any;

  beforeEach(() => {
    store = configureStore({
      reducer: {
        salaryTables: salaryTableReducer,
      },
    });
  });

  it('should handle initial state', () => {
    const initialState = store.getState().salaryTables;
    expect(initialState).toEqual({
      salaryTables: [],
      salaryTablesForGivenAgreement: [],
      status: 'idle',
      error: null,
      mode: 'create',
      currentSalaryTable: null,
    });
  });

  it('should handle switchToUpdateMode', () => {
    store.dispatch(switchToUpdateMode());
    const state = store.getState().salaryTables;
    expect(state.mode).toBe('update');
  });

  it('should handle switchToCreateMode', () => {
    store.dispatch(switchToUpdateMode()); // Switch to update first
    store.dispatch(switchToCreateMode());
    const state = store.getState().salaryTables;
    expect(state.mode).toBe('create');
  });

  it('should handle fetchAllSalaryTables thunk', async () => {
    window.mockElectronAPI.fetchAllSalaryTables.mockResolvedValueOnce([mockSalaryTable]);
    await store.dispatch(fetchAllSalaryTables());
    const state = store.getState().salaryTables;
    expect(state.salaryTables).toEqual([mockSalaryTable]);
    expect(state.status).toBe('succeeded');
  });

  it('should handle fetchSalaryTableById thunk', async () => {
    window.mockElectronAPI.fetchSalaryTableById.mockResolvedValueOnce(mockSalaryTable);
    await store.dispatch(fetchSalaryTableById(mockSalaryTable.salaryTableId!));
    const state = store.getState().salaryTables;
    expect(state.currentSalaryTable).toEqual(mockSalaryTable);
    expect(state.status).toBe('succeeded');
  });

  it('should handle fetchSalaryTablesByAgreementId thunk', async () => {
    window.mockElectronAPI.fetchSalaryTablesByAgreementId.mockResolvedValueOnce([mockSalaryTable]);
    await store.dispatch(fetchSalaryTablesByAgreementId(mockSalaryTable.agreementId!));
    const state = store.getState().salaryTables;
    expect(state.salaryTablesForGivenAgreement).toEqual([mockSalaryTable]);
    expect(state.status).toBe('succeeded');
  });

  it('should handle createSalaryTable thunk', async () => {
    window.mockElectronAPI.createSalaryTable.mockResolvedValueOnce(mockSalaryTable);
    await store.dispatch(createSalaryTable(mockSalaryTable));
    const state = store.getState().salaryTables;
    expect(state.salaryTables).toContainEqual(mockSalaryTable);
    expect(state.status).toBe('succeeded');
  });

  it('should handle updateSalaryTable thunk', async () => {
    const updatedSalaryTable = { ...mockSalaryTable, numeroTable: '124' };
    window.mockElectronAPI.updateSalaryTable.mockResolvedValueOnce(updatedSalaryTable);
    await store.dispatch(updateSalaryTable(updatedSalaryTable));
    const state = store.getState().salaryTables;
    expect(state.salaryTables).toContainEqual(updatedSalaryTable);
    expect(state.status).toBe('succeeded');
  });

  it('should handle deleteSalaryTable thunk', async () => {
    window.mockElectronAPI.deleteSalaryTable.mockResolvedValueOnce(mockSalaryTable);
    await store.dispatch(deleteSalaryTable(mockSalaryTable.salaryTableId!));
    const state = store.getState().salaryTables;
    expect(state.salaryTables).not.toContainEqual(mockSalaryTable);
    expect(state.status).toBe('succeeded');
  });
});
