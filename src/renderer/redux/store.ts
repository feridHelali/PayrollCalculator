// store.ts
import { configureStore } from '@reduxjs/toolkit';
import sectorialJointAgreementReducer from './sectorialJointAgreement/sectorialJointAgreementSlice';
import salaryTableReducer from './sectorialJointAgreement/salaryTableSlice';
import statusReducer from './common/statusSlice';

const store = configureStore({
  reducer: {
    agreements: sectorialJointAgreementReducer,
    salaryTables: salaryTableReducer,
    status: statusReducer,
  },
});

export type AppDispatch = typeof store.dispatch;
export type RootState = ReturnType<typeof store.getState>;

export default store;

