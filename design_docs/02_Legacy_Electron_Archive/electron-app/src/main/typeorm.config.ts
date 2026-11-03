import { DataSource } from 'typeorm';
import { join } from 'path';
import { ActualMonthSalary } from './db/entities/ActualMonthSalary';
import { Affair } from './db/entities/Affair';
import { SalaryTable } from './db/entities/SalaryTable';
import { SectorialJointAgreement } from './db/entities/SectorialJointAgreement';
import { SalaryTableCell } from './db/entities/SalaryTableCell';

const databasePath = join(__dirname, '..', 'payroll.sqlite');

const AppDataSource = new DataSource({
  type: 'better-sqlite3',
  database: databasePath,
  entities: [ActualMonthSalary, Affair, SectorialJointAgreement, SalaryTableCell, SalaryTable],
  synchronize: true,
  logging: true,
  logger: 'advanced-console',
  migrations: [
    "src/main/db/migrations/**/*.ts"
  ],
  subscribers: [
    "src/main/db/subscribers/**/*.ts"
  ]
});

export default AppDataSource;
