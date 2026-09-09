START TRANSACTION;

CREATE TABLE survey_assignments (
    id uuid NOT NULL,
    survey_id uuid NOT NULL,
    career_id uuid NOT NULL,
    subject_id uuid NOT NULL,
    academic_cycle_id uuid NOT NULL,
    teacher_subject_assignment_id uuid NOT NULL,
    is_active boolean NOT NULL,
    created_at_utc timestamp with time zone NOT NULL,
    updated_at_utc timestamp with time zone NOT NULL,
    CONSTRAINT "PK_survey_assignments" PRIMARY KEY (id),
    CONSTRAINT "FK_survey_assignments_academic_cycles_academic_cycle_id" FOREIGN KEY (academic_cycle_id) REFERENCES academic_cycles (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_survey_assignments_careers_career_id" FOREIGN KEY (career_id) REFERENCES careers (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_survey_assignments_subjects_subject_id" FOREIGN KEY (subject_id) REFERENCES subjects (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_survey_assignments_surveys_survey_id" FOREIGN KEY (survey_id) REFERENCES surveys (id) ON DELETE RESTRICT,
    CONSTRAINT "FK_survey_assignments_teacher_subject_assignments_teacher_subj~" FOREIGN KEY (teacher_subject_assignment_id) REFERENCES teacher_subject_assignments (id) ON DELETE RESTRICT
);

CREATE INDEX "IX_survey_assignments_academic_cycle_id" ON survey_assignments (academic_cycle_id);

CREATE INDEX "IX_survey_assignments_career_id" ON survey_assignments (career_id);

CREATE INDEX "IX_survey_assignments_subject_id" ON survey_assignments (subject_id);

CREATE INDEX "IX_survey_assignments_survey_id" ON survey_assignments (survey_id);

CREATE UNIQUE INDEX "IX_survey_assignments_survey_id_career_id_subject_id_academic_~" ON survey_assignments (survey_id, career_id, subject_id, academic_cycle_id, teacher_subject_assignment_id);

CREATE INDEX "IX_survey_assignments_teacher_subject_assignment_id" ON survey_assignments (teacher_subject_assignment_id);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260907164458_AddSurveyAssignments', '8.0.0');

COMMIT;

