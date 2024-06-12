// store.ts
import { configureStore } from '@reduxjs/toolkit';
import sectorialJointAgreementReducer from './sectorialJointAgreement/sectorialJointAgreementSlice';

const store = configureStore({
  reducer: {
    agreements: sectorialJointAgreementReducer,
  },
});

export type AppDispatch = typeof store.dispatch;
export type RootState = ReturnType<typeof store.getState>;

export default store;
