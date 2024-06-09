import 'reflect-metadata';
import { createConnection } from 'typeorm';
import { User } from './entities/User';

createConnection({
  type: 'sqlite',
  database: './database.sqlite',
  entities: [User],
  synchronize: true,
}).then(() => {
  console.log('Database connected');
}).catch(error => console.log(error));
