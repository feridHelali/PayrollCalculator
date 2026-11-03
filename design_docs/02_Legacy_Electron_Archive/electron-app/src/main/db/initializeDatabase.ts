import AppDataSource from '../../main/typeorm.config';
export default async function initializeDatabase() {
    try {
      await AppDataSource.initialize();
      console.log('Data Source has been initialized!');
    } catch (err) {
      console.error('Error during Data Source initialization:', err);
      throw err;
    }
  }