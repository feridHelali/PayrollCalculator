// sectorialJointAgreementSlice.ts
import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { sectorialJointAgreementProps } from '../../../types/sectorialAgreementProps';


interface AgreementsState {
  agreements: sectorialJointAgreementProps[];
  status: 'idle' | 'loading' | 'succeeded' | 'failed';
  error: string | null;
  mode: 'create' | 'update';
  currentAgreement: sectorialJointAgreementProps | null;
}

const initialState: AgreementsState = {
  agreements: [],
  status: 'idle',
  error: null,
  mode: 'create',
  currentAgreement: null,
};

export const fetchAgreements = createAsyncThunk<sectorialJointAgreementProps[]>(
  'agreements/fetchAgreements',
  async () => {
    return await window.electronAPI.fetchAgreements();
  }
);

export const fetchAgreementById = createAsyncThunk<sectorialJointAgreementProps, string>(
  'agreements/fetchAgreementById',
  async (sectorialJointAgreementId: string) => {
    return await window.electronAPI.fetchAgreementById(sectorialJointAgreementId);
  }
)

export const createAgreement = createAsyncThunk<sectorialJointAgreementProps, sectorialJointAgreementProps>(
  'agreements/createAgreement',
  async (agreement: sectorialJointAgreementProps) => {
    return await window.electronAPI.createAgreement(agreement);
  }
);

export const updateAgreement = createAsyncThunk<sectorialJointAgreementProps, sectorialJointAgreementProps>(
  'agreements/updateAgreement',
  async (agreement: sectorialJointAgreementProps) => {
    return await window.electronAPI.updateAgreement(agreement);
  }
);

export const deleteAgreement = createAsyncThunk<number, number>(
  'agreements/deleteAgreement',
  async (id: number) => {
    await window.electronAPI.deleteAgreement(id);
    return id;
  }
);

const sectorialJointAgreementSlice = createSlice({
  name: 'agreements',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchAgreements.pending, (state) => {
        state.status = 'loading';
      })
      .addCase(fetchAgreements.fulfilled, (state, action: PayloadAction<sectorialJointAgreementProps[]>) => {
        state.status = 'succeeded';
        state.agreements = action.payload;
      })
      .addCase(fetchAgreements.rejected, (state, action) => {
        state.status = 'failed';
        state.error = action.error.message ?? null;
      })
      .addCase(fetchAgreementById.pending, (state) => {
        state.status = 'loading';
      })
      .addCase(fetchAgreementById.fulfilled, (state, action: PayloadAction<sectorialJointAgreementProps>) => {
        state.currentAgreement = action.payload;
        state.status = 'succeeded';
        state.error = null;
        state.mode = 'update';
      })
      .addCase(fetchAgreementById.rejected, (state, action) => {
        state.status = 'failed';
        state.error = action.error.message ?? null;
      })
      .addCase(createAgreement.fulfilled, (state, action: PayloadAction<sectorialJointAgreementProps>) => {
        state.agreements.push(action.payload);
        state.status = 'succeeded';
        state.error = null;
        state.mode = 'create';
      })
      .addCase(updateAgreement.fulfilled, (state, action: PayloadAction<sectorialJointAgreementProps>) => {
        const index = state.agreements.findIndex(agreement => agreement.sectorialJointAgreementId === action.payload.sectorialJointAgreementId);
        if (index >= 0) {
          state.agreements[index] = action.payload;
        }
      })
      .addCase(deleteAgreement.fulfilled, (state, action: PayloadAction<number>) => {
        state.agreements = state.agreements.filter(agreement => agreement.sectorialJointAgreementId !== action.payload);
      });
  },
});

export default sectorialJointAgreementSlice.reducer;
