import { createSlice, PayloadAction } from '@reduxjs/toolkit';

interface StatusState {
  message: string;
}

const initialState: StatusState = {
  message: '',
};

const statusSlice = createSlice({
  name: 'status',
  initialState,
  reducers: {
    setStatus: (state, action: PayloadAction<string>) => {
      state.message = action.payload;
    },
    clearStatus: (state) => {
      state.message = '';
    },
  },
});

export const { setStatus, clearStatus } = statusSlice.actions;

export default statusSlice.reducer;
