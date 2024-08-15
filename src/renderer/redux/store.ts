// store.ts
import { configureStore } from '@reduxjs/toolkit';
import sectorialJointAgreementReducer from './sectorialJointAgreement/sectorialJointAgreementSlice';
import salaryTableReducer from './sectorialJointAgreement/salaryTablesSlice';
import statusReducer from './common/statusSlice';
import AffairReducer from './affair/affairSlice';

const store = configureStore({
  reducer: {
    agreements: sectorialJointAgreementReducer,
    salaryTables: salaryTableReducer,
    status: statusReducer,
    affairs: AffairReducer,
  },
});

export type AppDispatch = typeof store.dispatch;
export type RootState = ReturnType<typeof store.getState>;

export default store;

