import { SqliteDriver } from 'typeorm/driver/sqlite/SqliteDriver';
import { join } from 'path';
import { ActualMonthSalary } from './db/entities/ActualMonthSalary'; 
import { Affair } from './db/entities/Affair'; 
import { AgreementApplicationPeriods } from './db/entities/AgreementApplicationPeriods'; 
import { SalaryTable } from './db/entities/SalaryTable'; 
import { SectorialJointAgreement } from './db/entities/SectorialJointAgreement'; 
import { SalaryTableCell } from './db/entities/SalaryTableCell';
import { DataSource } from 'typeorm';

console.log('NODE_ENV:', process.env.NODE_ENV);
console.log('sqlite3 path:', require.resolve('sqlite3'));

const AppDataSource = new DataSource({
    type: 'sqlite',
    database: join(__dirname, '..', 'payroll.db'),
    entities: [ActualMonthSalary, Affair, SectorialJointAgreement, AgreementApplicationPeriods, SalaryTableCell, SalaryTable], 
    synchronize: true,
    logging:true,
    logger: 'advanced-console',
});

export default AppDataSource;
