import { Entity, PrimaryGeneratedColumn, Column } from 'typeorm';

@Entity()
export class User {
  @PrimaryGeneratedColumn()
  id!: number;

  @Column()
  name!: string;

  @Column()
  email!: string;

  @Column()
  salary!: number;

  constructor(name: string, email: string, salary: number) {
    this.name = name;
    this.email = email;
    this.salary = salary;
  }
}
