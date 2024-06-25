import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { SalaryTableProps } from '../../../types/salaryTableProps';

interface SalaryTablesState {
  salaryTables: SalaryTableProps[];
  salaryTablesForGivenAgreement: SalaryTableProps[];
  status: 'idle' | 'loading' | 'succeeded' | 'failed';
  error: string | null;
  mode: 'create' | 'update';
  currentSalaryTable: SalaryTableProps | null;
}

const initialState: SalaryTablesState = {
  salaryTables: [],
  salaryTablesForGivenAgreement: [],
  status: 'idle',
  error: null,
  mode: 'create',
  currentSalaryTable: null,
};

export const fetchAllSalaryTables = createAsyncThunk<SalaryTableProps>(
  'salaryTables/fetchAllSalaryTables',
  async () => {
    return await window.electronAPI.fetchAllSalaryTables();
  }
  
)

export const fetchSalaryTableById = createAsyncThunk<SalaryTableProps, number>(
  'salaryTables/fetchSalaryTableById',
  async (salaryTableId: number) => {
    return await window.electronAPI.fetchSalaryTableById(salaryTableId);
  } 
)

export const fetchSalaryTablesByAgreementId = createAsyncThunk<SalaryTableProps[],number>(
  'salaryTables/fetchSalaryTablesByAgreementId',
  async (agreementId:number) => {
    return await window.electronAPI.fetchSalaryTablesByAgreementId(agreementId);
  }
);

export const createSalaryTable = createAsyncThunk<SalaryTableProps, Partial<SalaryTableProps>>(
  'salaryTables/createSalaryTable',
  async (salaryTable: Partial<SalaryTableProps>) => {
    const result = await window.electronAPI.createSalaryTable(salaryTable);
    return result;
  }
);

export const updateSalaryTable = createAsyncThunk<SalaryTableProps, Partial<SalaryTableProps>>(
  'salaryTables/updateSalaryTable',
  async (salaryTable: Partial<SalaryTableProps>) => {
    return await window.electronAPI.updateSalaryTable(salaryTable);
  }     
)

export const deleteSalaryTable = createAsyncThunk<SalaryTableProps, number>(
  'salaryTables/deleteSalaryTable',
  async (salaryTableId: number) => {
    return await window.electronAPI.deleteSalaryTable(salaryTableId);
  }
)

const salaryTableSlice = createSlice({
  name: 'salaryTables',
  initialState,
  reducers: {
    switchToUpdateMode: (state) => {
      state.mode = 'update';
    },
    switchToCreateMode: (state) => {
      state.mode = 'create';
    }
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchSalaryTablesByAgreementId.pending, (state) => {
        state.status = 'loading';
      })
      .addCase(fetchSalaryTablesByAgreementId.fulfilled, (state, action: PayloadAction<SalaryTableProps[]>) => {
        state.status = 'succeeded';
        state.salaryTablesForGivenAgreement = action.payload;
      })
      .addCase(fetchSalaryTablesByAgreementId.rejected, (state, action) => {
        state.status = 'failed';
        state.error = action.error.message ?? null;
      })
      .addCase(fetchAllSalaryTables.pending, (state) => {
        state.status = 'loading';
      })
      .addCase(fetchAllSalaryTables.rejected, (state, action) => {
        state.status = 'failed';
        state.error = action.error.message ?? null;
      })
      .addCase(fetchAllSalaryTables.fulfilled, (state, action: PayloadAction<any>) => {
        state.status = 'succeeded';
        state.salaryTables = action.payload;
      })
      .addCase(fetchSalaryTableById.pending, (state) => {
        state.status = 'loading';
      })
      .addCase(fetchSalaryTableById.fulfilled, (state, action: PayloadAction<SalaryTableProps>) => {
        state.status = 'succeeded';
        state.currentSalaryTable = action.payload;
      })
      .addCase(fetchSalaryTableById.rejected, (state, action) => {
        state.status = 'failed';
        state.error = action.error.message ?? null;
      })
      .addCase(createSalaryTable.fulfilled, (state, action: PayloadAction<SalaryTableProps>) => {
        state.salaryTables.push(action.payload);
        state.status = 'succeeded';
        state.error = null;
      });
  },
});

export default salaryTableSlice.reducer;
export const { switchToCreateMode, switchToUpdateMode } = salaryTableSlice.actions;