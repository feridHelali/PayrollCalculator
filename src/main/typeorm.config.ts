
import { ActualMonthSalary } from './db/entities/ActualMonthSalary'; 
import { Affair } from './db/entities/Affair'; 
import { AgreementApplicationPeriods } from './db/entities/AgreementApplicationPeriods'; 
import { SalaryTable } from './db/entities/SalaryTable'; 
import { SectorialJointAgreement } from './db/entities/SectorialJointAgreement'; 
import { SalaryTableCell } from './db/entities/SalaryTableCell';
import { DataSource } from 'typeorm';
import { join } from 'path';



const AppDataSource = new DataSource({
    type: 'better-sqlite3',
    database: join(__dirname, '../../payroll.sqlite'),
    entities: [ActualMonthSalary, Affair, SectorialJointAgreement, AgreementApplicationPeriods, SalaryTableCell, SalaryTable], 
    synchronize: false,
    logging:true,
    logger: 'advanced-console',
});

export default AppDataSource;
