
import { DataSource } from 'typeorm';
import { ActualMonthSalary } from './db/entities/ActualMonthSalary'; 
import { Affair } from './db/entities/Affair'; 
import { AgreementApplicationPeriods } from './db/entities/AgreementApplicationPeriods'; 
import { SalaryTable } from './db/entities/SalaryTable'; 
import { SectorialJointAgreement } from './db/entities/SectorialJointAgreement'; 
import { SalaryTableCell } from './db/entities/SalaryTableCell';
import { SqliteDriver } from 'typeorm/driver/sqlite/SqliteDriver';

console.log('NODE_ENV:', process.env.NODE_ENV);
console.log('sqlite3 path:', require.resolve('sqlite3'));

const AppDataSource = new DataSource({
    type: 'sqlite',
    database: '../../payroll.db',
    entities: [ActualMonthSalary, Affair,SectorialJointAgreement,AgreementApplicationPeriods,SalaryTableCell,SalaryTable], // Add all your entity classes here
    synchronize: true,
});

export default AppDataSource;
