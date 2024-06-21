// store.ts
import { configureStore } from '@reduxjs/toolkit';
import sectorialJointAgreementReducer from './sectorialJointAgreement/sectorialJointAgreementSlice';
import salaryTableReducer from './sectorialJointAgreement/salaryTableSlice';


const store = configureStore({
  reducer: {
    agreements: sectorialJointAgreementReducer,
    salaryTables: salaryTableReducer,
  },
});

export type AppDispatch = typeof store.dispatch;
export type RootState = ReturnType<typeof store.getState>;

export default store;

