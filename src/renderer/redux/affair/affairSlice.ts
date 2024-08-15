// store/affairSlice.ts
import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';

interface AffairState {
  affairs: any[];
  currentAffair: any | null;
  status: 'idle' | 'loading' | 'succeeded' | 'failed';
  error: string | null;
  mode: 'create' | 'update';
}

const initialState: AffairState = {
  affairs: [],
  currentAffair: null,
  status: 'idle',
  error: null,
  mode: 'create',
};

export const fetchAffairs = createAsyncThunk('affairs/fetchAffairs', async () => {
  return await window.electronAPI.fetchAffairs();
});

export const fetchAffairById = createAsyncThunk('affairs/fetchAffairById', async (id: string) => {
  return await window.electronAPI.fetchAffairById(id);
});

export const createAffair = createAsyncThunk('affairs/createAffair', async (affair: any) => {
  return await window.electronAPI.createAffair(affair);
});

export const updateAffair = createAsyncThunk('affairs/updateAffair', async (affair: any) => {
  return await window.electronAPI.updateAffair(affair);
});

export const deleteAffair = createAsyncThunk('affairs/deleteAffair', async (id: number) => {
  return await window.electronAPI.deleteAffair(id);
});

const affairSlice = createSlice({
  name: 'affairs',
  initialState,
  reducers: {
    switchToCreateMode: (state) => {
      state.mode = 'create';
    },
    switchToUpdateMode: (state) => {
      state.mode = 'update';
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchAffairs.pending, (state) => {
        state.status = 'loading';
      })
      .addCase(fetchAffairs.fulfilled, (state, action: PayloadAction<any[]>) => {
        state.status = 'succeeded';
        state.affairs = action.payload;
      })
      .addCase(fetchAffairs.rejected, (state, action) => {
        state.status = 'failed';
        state.error = action.error.message ?? null;
      })
      .addCase(createAffair.fulfilled, (state, action: PayloadAction<any>) => {
        state.affairs.push(action.payload);
        state.status = 'succeeded';
      })
      .addCase(updateAffair.fulfilled, (state, action: PayloadAction<any>) => {
        state.status = 'succeeded';
        state.currentAffair = action.payload;
      })
      .addCase(deleteAffair.fulfilled, (state, action: PayloadAction<number>) => {
        state.affairs = state.affairs.filter((affair) => affair.affairId !== action.payload);
        state.status = 'succeeded';
      });
  },
});

export default affairSlice.reducer;
export const { switchToCreateMode, switchToUpdateMode } = affairSlice.actions;
