import 'reflect-metadata';
import { createConnection } from 'typeorm';
import { User } from './entities/User';
import { Category } from './entities/Category';
import { ConventionCollective } from './entities/ConventionCollective';
import { Echelon } from './entities/Echelon';
import { SalaryMatrix } from './entities/SalaryMatrix';

createConnection({
  type: 'sqlite',
  database: './database.sqlite',
  entities: [User,Category,ConventionCollective,Echelon,SalaryMatrix],
  synchronize: true,
}).then(() => {
  console.log('Database connected');
}).catch(error => console.log(error));
