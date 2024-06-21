import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { SalaryTableProps } from '../../../types/salaryTableProps';

interface SalaryTablesState {
  salaryTables: SalaryTableProps[];
  status: 'idle' | 'loading' | 'succeeded' | 'failed';
  error: string | null;
}

const initialState: SalaryTablesState = {
  salaryTables: [],
  status: 'idle',
  error: null,
};

export const fetchSalaryTables = createAsyncThunk<SalaryTableProps[],string>(
  'salaryTables/fetchSalaryTables',
  async (id:string) => {
    return await window.electronAPI.fetchSalaryTable(id);
  }
);

export const createSalaryTable = createAsyncThunk<SalaryTableProps, Partial<SalaryTableProps>>(
  'salaryTables/createSalaryTable',
  async (salaryTable: Partial<SalaryTableProps>) => {
    return await window.electronAPI.createSalaryTable(salaryTable);
  }
);

export const updateSalaryTable = createAsyncThunk<SalaryTableProps, Partial<SalaryTableProps>>(
  'salaryTables/updateSalaryTable',
  async (salaryTable: Partial<SalaryTableProps>) => {
    return await window.electronAPI.updateSalaryTable(salaryTable);
  }     
)

export const deleteSalaryTable = createAsyncThunk<SalaryTableProps, string>(
  'salaryTables/deleteSalaryTable',
  async (salaryTableId: string) => {
    return await window.electronAPI.deleteSalaryTable(salaryTableId);
  }
)

const salaryTableSlice = createSlice({
  name: 'salaryTables',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchSalaryTables.pending, (state) => {
        state.status = 'loading';
      })
      .addCase(fetchSalaryTables.fulfilled, (state, action: PayloadAction<SalaryTableProps[]>) => {
        state.status = 'succeeded';
        state.salaryTables = action.payload;
      })
      .addCase(fetchSalaryTables.rejected, (state, action) => {
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
