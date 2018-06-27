drop table lesson_signal;

CREATE TABLE lesson_signal (
     id INT NOT NULL AUTO_INCREMENT,
     timestamp_ TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
     signal_type SMALLINT,
     student_id INT NOT NULL,
     PRIMARY KEY (id, student_id)
     INDEX FK_lesson_signal_student_idx (student_id ASC),
     CONSTRAINT FK_lesson_signal_student
     FOREIGN KEY (student_id) REFERENCES ucubot.student (id)
     ON DELETE RESTRICT
     ON UPDATE RESTRICT
     
);
